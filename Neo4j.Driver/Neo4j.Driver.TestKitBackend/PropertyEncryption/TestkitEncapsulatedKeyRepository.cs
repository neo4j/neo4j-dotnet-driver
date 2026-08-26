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
    private readonly Dictionary<string, EncapsulatedKey> _keysById = new();

    public EncapsulatedKey Import(
        string id,
        string alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        lock (_lock)
        {
            return Store(id, alias, encapsulation, metadata);
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
            var id = GenerateRandomKeyId();
            var key = Store(id, alias, encapsulation, metadata);
            return Task.FromResult(key);
        }
    }

    private EncapsulatedKey Store(
        string id,
        string? alias,
        byte[] encapsulation,
        IReadOnlyDictionary<string, string> metadata)
    {
        if (alias != null)
        {
            RemoveExistingAlias(alias);
        }

        var key = new EncapsulatedKey(id, alias, encapsulation, metadata);
        _keysById[id] = key;
        return key;
    }

    public Task<EncapsulatedKey> FindAsync(KeyReference keyReference, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(GetByReferenceOrThrow(keyReference));
        }
    }

    private static string GenerateRandomKeyId()
    {
        Span<byte> buffer = stackalloc byte[8];
        Random.Shared.NextBytes(buffer);
        return Convert.ToHexStringLower(buffer);
    }

    private EncapsulatedKey GetByReferenceOrThrow(KeyReference keyReference)
    {
        var (reference, type) = keyReference;

        var matchedKey = _keysById.Values
            .FirstOrDefault(k =>
                (type == KeyReferenceType.Id && k.Id == reference) ||
                (type == KeyReferenceType.Alias && k.Alias == reference));

        return matchedKey ??
            throw type switch
            {
                KeyReferenceType.Id => new EncapsulatedKeyNotFoundException(reference),
                KeyReferenceType.Alias => new EncapsulatedAliasNotFoundException(reference),
                _ => new ArgumentOutOfRangeException(nameof(keyReference), $"Unknown key reference type: {type}")
            };
    }

    private void RemoveExistingAlias(string alias)
    {
        foreach (var (id, encapsulatedKey) in _keysById)
        {
            if (encapsulatedKey.Alias != alias)
            {
                continue;
            }

            _keysById[id] = encapsulatedKey with { Alias = null };
            return;
        }
    }

    public Task AddAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeleteAliasByIdAsync(string id, string alias, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
