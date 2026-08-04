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

using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EnvelopeDataKeyProvider : IEnvelopeDataKeyProvider
{
    private const int DataKeyLength = 32;

    private readonly IAliasToKeyIdCache _aliasToKeyIdCache;
    private readonly IEncryptionKeyCache _encryptionKeyCache;
    private readonly IKeyDerivation _keyDerivation;

    public EnvelopeDataKeyProvider(
        IAliasToKeyIdCache aliasToKeyIdCache,
        IEncryptionKeyCache encryptionKeyCache,
        IKeyDerivation keyDerivation)
    {
        _aliasToKeyIdCache = aliasToKeyIdCache;
        _encryptionKeyCache = encryptionKeyCache;
        _keyDerivation = keyDerivation;
    }

    public async Task<DataKeyResult> GetDataKeyAsync(
        IEnvelopeEncryptionProfile profile,
        KeyReference keyRef,
        CancellationToken cancellationToken)
    {
        var (keyId, prefetchedKey) = await ResolveKeyIdAsync(profile, keyRef, cancellationToken).ConfigureAwait(false);
        var dek = await ResolveDataEncryptionKeyAsync(profile, keyId, prefetchedKey, cancellationToken)
            .ConfigureAwait(false);

        return new DataKeyResult(keyId, _keyDerivation.Derive(dek, DataKeyLength));
    }

    private async Task<(string KeyId, EncapsulatedKey? PrefetchedKey)> ResolveKeyIdAsync(
        IEnvelopeEncryptionProfile profile,
        KeyReference keyRef,
        CancellationToken cancellationToken)
    {
        if (keyRef.Type == KeyReferenceType.Id)
        {
            return (keyRef.Reference, null);
        }

        if (_aliasToKeyIdCache.TryGet(profile.Name, keyRef.Reference, out var cachedKeyId))
        {
            return (cachedKeyId, null);
        }

        var key = await profile.KeyRepository.FindAsync(keyRef, cancellationToken).ConfigureAwait(false);
        _aliasToKeyIdCache.Set(profile.Name, keyRef.Reference, key.Id);
        return (key.Id, key);
    }

    private async Task<byte[]> ResolveDataEncryptionKeyAsync(
        IEnvelopeEncryptionProfile profile,
        string keyId,
        EncapsulatedKey? prefetchedKey,
        CancellationToken cancellationToken)
    {
        if (_encryptionKeyCache.TryGet(profile.Name, keyId, out var cached))
        {
            return cached;
        }

        var key = prefetchedKey ?? await profile.KeyRepository
            .FindAsync(new KeyReference(keyId, KeyReferenceType.Id), cancellationToken)
            .ConfigureAwait(false);

        var dek = await profile.KeyEncapsulationService
            .DecapsulateAsync(key.Encapsulation, key.Metadata, cancellationToken)
            .ConfigureAwait(false);

        _encryptionKeyCache.Set(profile.Name, keyId, dek);
        return dek;
    }
}
