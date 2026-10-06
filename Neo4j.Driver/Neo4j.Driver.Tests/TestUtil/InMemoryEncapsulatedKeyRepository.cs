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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Tests.TestUtil;

public class InMemoryEncapsulatedKeyRepository : IEncapsulatedKeyRecordRepository
{
    private readonly IKeyIdGenerator _keyIdGenerator;
    private readonly object _lock = new();
    private readonly Dictionary<string, EncapsulatedKeyRecord> _idToKey = new();
    private readonly Dictionary<string, string> _aliasToId = new();

    public InMemoryEncapsulatedKeyRepository(IKeyIdGenerator keyIdGenerator)
    {
        _keyIdGenerator = keyIdGenerator;
    }

    public Task<EncapsulatedKeyRecord?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_idToKey.TryGetValue(id, out var key) ? key : null);
        }
    }

    public Task<EncapsulatedKeyRecord?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _aliasToId.TryGetValue(alias, out var id) && _idToKey.TryGetValue(id, out var key)
                ? key
                : null;

            return Task.FromResult(found);
        }
    }

    public Task<EncapsulatedKeyRecord> CreateAsync(
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
                EnsureAliasIsFree(alias, id);
                _aliasToId[alias] = id;
            }

            var key = new EncapsulatedKeyRecord(id, alias, encapsulation, metadata);
            _idToKey[id] = key;
            return Task.FromResult(key);
        }
    }

    public Task SetAliasByIdAsync(string id, string? alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var key = GetKeyByIdOrThrow(id);
            if (alias is not null)
            {
                EnsureAliasIsFree(alias, id);
            }

            if (key.Alias is not null)
            {
                _aliasToId.Remove(key.Alias);
            }

            if (alias is not null)
            {
                _aliasToId[alias] = id;
            }

            _idToKey[id] = key with { Alias = alias };
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
            return Task.CompletedTask;
        }
    }

    private void EnsureAliasIsFree(string alias, string idClaimingIt)
    {
        if (_aliasToId.TryGetValue(alias, out var owner) && owner != idClaimingIt)
        {
            throw new EncapsulatedAliasInUseException(alias);
        }
    }

    private EncapsulatedKeyRecord GetKeyByIdOrThrow(string id)
    {
        return _idToKey.TryGetValue(id, out var key) ? key : throw new EncapsulatedKeyNotFoundException(id);
    }
}
