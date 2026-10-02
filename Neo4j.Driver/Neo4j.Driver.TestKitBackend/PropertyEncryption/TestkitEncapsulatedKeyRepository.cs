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

using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.TestKitBackend.PropertyEncryption;

internal class TestkitEncapsulatedKeyRepository : ITestkitEncapsulatedKeyRepository
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, EncapsulatedKeyRecord> _keysById = new();

    public Task<EncapsulatedKeyRecord> ImportAsync(
        string id,
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        lock (_lock)
        {
            return Task.FromResult(Store(id, alias, encapsulation, metadata));
        }
    }

    public Task<EncapsulatedKeyRecord?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_keysById.TryGetValue(id, out var key) ? key : null);
        }
    }

    public Task<EncapsulatedKeyRecord?> FindByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<EncapsulatedKeyRecord?>(
                _keysById.Values.FirstOrDefault(k => k.Alias == alias));
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
            return Task.FromResult(Store(GenerateRandomKeyId(), alias, encapsulation, metadata));
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

            _keysById[id] = key with { Alias = alias };
            return Task.CompletedTask;
        }
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            GetKeyByIdOrThrow(id);
            _keysById.Remove(id);
            return Task.CompletedTask;
        }
    }

    private EncapsulatedKeyRecord Store(
        string id,
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        if (alias is not null)
        {
            EnsureAliasIsFree(alias, id);
        }

        var key = new EncapsulatedKeyRecord(id, alias, encapsulation, metadata);
        _keysById[id] = key;
        return key;
    }

    private static string GenerateRandomKeyId()
    {
        Span<byte> buffer = stackalloc byte[8];
        Random.Shared.NextBytes(buffer);
        return Convert.ToHexStringLower(buffer);
    }

    private void EnsureAliasIsFree(string alias, string idClaimingIt)
    {
        var owner = _keysById.Values.FirstOrDefault(k => k.Alias == alias);
        if (owner is not null && owner.Id != idClaimingIt)
        {
            throw new EncapsulatedAliasInUseException(alias);
        }
    }

    private EncapsulatedKeyRecord GetKeyByIdOrThrow(string id)
    {
        return _keysById.TryGetValue(id, out var key) ? key : throw new EncapsulatedKeyNotFoundException(id);
    }
}
