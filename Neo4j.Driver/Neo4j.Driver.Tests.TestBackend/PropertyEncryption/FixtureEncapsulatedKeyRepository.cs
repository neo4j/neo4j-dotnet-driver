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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Tests.TestBackend.PropertyEncryption;

internal class FixtureEncapsulatedKeyRepository : IEncapsulatedKeyRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<string, EncapsulatedKey> _idToKey = new();
    private readonly Dictionary<string, string> _aliasToId = new();
    private int _nextId;

    public Task<EncapsulatedKey> FindAsync(KeyReference keyReference, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(GetByIdOrThrow(ResolveId(keyReference)));
        }
    }

    public Task<EncapsulatedKey> SaveAsync(
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (alias != null)
            {
                RemoveExistingAlias(alias);
            }

            var id = (_nextId++).ToString();
            var key = new EncapsulatedKey(id, alias, encapsulation, metadata);
            _idToKey[id] = key;
            if (alias != null)
            {
                _aliasToId[alias] = id;
            }

            return Task.FromResult(key);
        }
    }

    public EncapsulatedKey Import(
        string id,
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        lock (_lock)
        {
            RemoveExistingAlias(alias);
            var key = new EncapsulatedKey(id, alias, encapsulation, metadata);
            _idToKey[id] = key;
            _aliasToId[alias] = id;
            return key;
        }
    }

    public Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetByIdOrThrow(id);
            RemoveExistingAlias(alias);
            _aliasToId[alias] = id;
            _idToKey[id] = key with { Alias = alias };
            return Task.CompletedTask;
        }
    }

    public Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetByIdOrThrow(id);
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
            var key = GetByIdOrThrow(id);
            if (key.Alias != null)
            {
                _aliasToId.Remove(key.Alias);
            }

            _idToKey.Remove(id);
            return Task.CompletedTask;
        }
    }

    private string ResolveId(KeyReference keyReference)
    {
        if (keyReference.Type == KeyReferenceType.Id)
        {
            return keyReference.Reference;
        }

        if (!_aliasToId.TryGetValue(keyReference.Reference, out var id))
        {
            throw new EncapsulatedAliasNotFoundException(keyReference.Reference);
        }

        return id;
    }

    private EncapsulatedKey GetByIdOrThrow(string id)
    {
        if (!_idToKey.TryGetValue(id, out var key))
        {
            throw new EncapsulatedKeyNotFoundException(id);
        }

        return key;
    }

    private void RemoveExistingAlias(string alias)
    {
        if (_aliasToId.Remove(alias, out var previousId) && _idToKey.TryGetValue(previousId, out var previousKey))
        {
            _idToKey[previousId] = previousKey with { Alias = null };
        }
    }
}
