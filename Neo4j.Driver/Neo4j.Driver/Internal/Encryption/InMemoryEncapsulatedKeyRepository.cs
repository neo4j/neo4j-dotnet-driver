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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class InMemoryEncapsulatedKeyRepository : IEncapsulatedKeyRepository
{
    private readonly IKeyIdGenerator _keyIdGenerator;
    private readonly object _lock = new();
    private readonly Dictionary<string, EncapsulatedKey> _idToKey = new();
    private readonly Dictionary<string, string> _aliasToId = new();

    public InMemoryEncapsulatedKeyRepository(IKeyIdGenerator keyIdGenerator)
    {
        _keyIdGenerator = keyIdGenerator;
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
                    nameof(keyReference),
                    keyReference.Type,
                    "Unknown key reference type")
            };

            return Task.FromResult(GetKeyByIdOrThrow(id));
        }
    }

    public Task<EncapsulatedKey> SaveAsync(
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var id = _keyIdGenerator.Get();
            if (alias is not null)
            {
                RemoveExistingAlias(alias);
            }

            var key = new EncapsulatedKey(id, alias, encapsulation, metadata);
            _idToKey[id] = key;
            if (alias is not null)
            {
                _aliasToId[alias] = id;
            }

            return Task.FromResult(key);
        }
    }

    public Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            RemoveExistingAlias(alias);
            if (key.Alias is not null)
            {
                _aliasToId.Remove(key.Alias);
            }

            _aliasToId[alias] = id;
            _idToKey[id] = key with { Alias = alias };
            return Task.CompletedTask;
        }
    }

    public Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            if (key.Alias != alias)
            {
                throw new EncapsulatedAliasNotFoundException(alias);
            }

            _aliasToId.Remove(alias);
            _idToKey[id] = key with { Alias = null };
            return Task.CompletedTask;
        }
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            if (key.Alias is not null)
            {
                _aliasToId.Remove(key.Alias);
            }

            _idToKey.Remove(id);
        }

        return Task.CompletedTask;
    }

    private EncapsulatedKey GetKeyByIdOrThrow(string id)
    {
        // assumes lock already held
        return _idToKey.TryGetValue(id, out var key)
            ? key
            : throw new EncapsulatedKeyNotFoundException(id);
    }

    private void RemoveExistingAlias(string alias)
    {
        // assumes lock already held
        if (!_aliasToId.TryGetValue(alias, out var prevKeyId))
        {
            return;
        }

        _idToKey[prevKeyId] = _idToKey[prevKeyId] with { Alias = null };
    }
}
