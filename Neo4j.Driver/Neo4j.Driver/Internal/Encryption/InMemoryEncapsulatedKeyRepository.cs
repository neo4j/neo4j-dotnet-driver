// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Encryption;

internal class InMemoryEncapsulatedKeyRepository : IEncapsulatedKeyRepository
{
    private readonly IKeyIdGenerator _keyIdGenerator;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, EncapsulatedKey> _idToKey = new();
    private readonly ConcurrentDictionary<string, string> _aliasToId = new();

    public InMemoryEncapsulatedKeyRepository(IKeyIdGenerator keyIdGenerator)
    {
        _keyIdGenerator = keyIdGenerator;
    }

    private EncapsulatedKey GetKeyByIdOrThrow(string id)
    {
        lock (_lock)
        {
            return _idToKey.TryGetValue(id, out var key)
                ? key
                : throw new EncapsulatedKeyNotFoundException(id);
        }
    }

    public Task<EncapsulatedKey> FindAsync(KeyReference keyReference, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var id = keyReference switch
            {
                { Type: KeyReferenceType.Id } => keyReference.Reference,

                { Type: KeyReferenceType.Alias } => _aliasToId.TryGetValue(keyReference.Reference, out var k) 
                    ? k
                    : throw new EncapsulatedAliasNotFoundException(keyReference.Reference),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(keyReference), keyReference.Type, "Unknown key reference type")
            };

            return Task.FromResult(GetKeyByIdOrThrow(id));
        }
    }

    public Task<EncapsulatedKey> SaveAsync(
        IEnumerable<string> aliases,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var id = _keyIdGenerator.Get();
            var aliasSet = aliases.ToHashSet();
            RemoveExistingAliases(aliasSet);
            var key = new EncapsulatedKey(id, aliasSet, encapsulation, metadata);
            _idToKey.AddOrUpdate(id, key, (_, _) => key);
            foreach (var alias in aliasSet)
            {
                _aliasToId.AddOrUpdate(alias, id, (_, _) => id);
            }

            return Task.FromResult(key);
        }
    }

    public Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            RemoveExistingAliases([alias]);

            var gainingKey = key with { Aliases = key.Aliases + [alias] };
            _aliasToId.AddOrUpdate(alias, id, (_, _) => id);
            _idToKey.AddOrUpdate(id, gainingKey, (_, _) => gainingKey);
            return Task.CompletedTask;
        }
    }

    private void RemoveExistingAliases(IEnumerable<string> aliases)
    {
        lock (_lock)
        {
            foreach (var alias in aliases)
            {
                if (!_aliasToId.TryGetValue(alias, out var prevKeyId))
                {
                    continue;
                }

                var existingKey = GetKeyByIdOrThrow(prevKeyId);
                var updatedKey = existingKey with { Aliases = existingKey.Aliases - [alias] };
                _idToKey.AddOrUpdate(prevKeyId, updatedKey, (_, _) => updatedKey);
            }
        }
    }

    public Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            if (!key.Aliases.Contains(alias))
            {
                throw new EncapsulatedAliasNotFoundException(alias);
            }

            var newAliases = key.Aliases.Where(a => a != alias).ToHashSet();
            var newKey = key with { Aliases = newAliases };
            _aliasToId.TryRemove(alias, out _);
            _idToKey.AddOrUpdate(id, newKey, (_, _) => newKey);
            return Task.CompletedTask;
        }
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            foreach (var alias in key.Aliases)
            {
                _aliasToId.TryRemove(alias, out _);
            }

            _idToKey.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }
}
