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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

internal class EnvelopeDataKeyProvider : IEnvelopeDataKeyProvider
{
    private readonly IAliasToKeyIdCache _aliasToKeyIdCache;
    private readonly IEncryptionKeyCache _encryptionKeyCache;
    private readonly ConcurrentDictionary<(string ProfileName, string KeyId), Task<byte[]>> _decapsulationsInFlight = new();

    public EnvelopeDataKeyProvider(IAliasToKeyIdCache aliasToKeyIdCache, IEncryptionKeyCache encryptionKeyCache)
    {
        _aliasToKeyIdCache = aliasToKeyIdCache;
        _encryptionKeyCache = encryptionKeyCache;
    }

    public async Task<DataKeyResult> GetDataKeyAsync(
        IEnvelopeEncryptionProfile profile,
        KeyReference keyRef,
        CancellationToken cancellationToken)
    {
        if (keyRef.Type == KeyReferenceType.Id)
        {
            return await GetDataKeyByIdAsync(profile, keyRef.Reference, cancellationToken).ConfigureAwait(false);
        }

        return await GetDataKeyByAliasAsync(profile, keyRef.Reference, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataKeyResult> GetDataKeyByIdAsync(
        IEnvelopeEncryptionProfile profile,
        string keyId,
        CancellationToken cancellationToken)
    {
        if (_encryptionKeyCache.TryGet(profile, keyId, out var cachedDek))
        {
            return new DataKeyResult(keyId, cachedDek);
        }

        var key = await profile.KeyRepository.FindByIdAsync(keyId, cancellationToken).ConfigureAwait(false)
            ?? throw new EncapsulatedKeyNotFoundException(keyId);

        return await GetOrDecapsulateDataKeyAsync(profile, key, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataKeyResult> GetDataKeyByAliasAsync(
        IEnvelopeEncryptionProfile profile,
        string alias,
        CancellationToken cancellationToken)
    {
        if (_aliasToKeyIdCache.TryGet(profile, alias, out var indexedKeyId))
        {
            if (_encryptionKeyCache.TryGet(profile, indexedKeyId, out var cachedDek))
            {
                return new DataKeyResult(indexedKeyId, cachedDek);
            }

            _aliasToKeyIdCache.Remove(profile, alias);
        }

        var key = await profile.KeyRepository.FindByAliasAsync(alias, cancellationToken).ConfigureAwait(false)
            ?? throw new EncapsulatedAliasNotFoundException(alias);

        _aliasToKeyIdCache.Set(profile, alias, key.Id);

        return await GetOrDecapsulateDataKeyAsync(profile, key, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataKeyResult> GetOrDecapsulateDataKeyAsync(
        IEnvelopeEncryptionProfile profile,
        EncapsulatedKeyRecord key,
        CancellationToken cancellationToken)
    {
        if (_encryptionKeyCache.TryGet(profile, key.Id, out var cachedDek))
        {
            return new DataKeyResult(key.Id, cachedDek);
        }

        var dek = await DecapsulateOnceAsync(profile, key).WaitAsync(cancellationToken).ConfigureAwait(false);
        return new DataKeyResult(key.Id, dek);
    }

    private Task<byte[]> DecapsulateOnceAsync(IEnvelopeEncryptionProfile profile, EncapsulatedKeyRecord key)
    {
        var slot = (profile.Name, key.Id);
        var decapsulation = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var inFlight = _decapsulationsInFlight.GetOrAdd(slot, decapsulation.Task);

        if (inFlight == decapsulation.Task)
        {
            _ = CompleteDecapsulationAsync(decapsulation, slot, profile, key);
        }

        return inFlight;
    }

    private async Task CompleteDecapsulationAsync(
        TaskCompletionSource<byte[]> decapsulation,
        (string ProfileName, string KeyId) slot,
        IEnvelopeEncryptionProfile profile,
        EncapsulatedKeyRecord key)
    {
        try
        {
            decapsulation.SetResult(await DecapsulateAndCacheAsync(profile, key).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            decapsulation.SetException(e);
        }
        finally
        {
            _decapsulationsInFlight.TryRemove(KeyValuePair.Create(slot, decapsulation.Task));
        }
    }

    private async Task<byte[]> DecapsulateAndCacheAsync(IEnvelopeEncryptionProfile profile, EncapsulatedKeyRecord key)
    {
        var dek = await profile.KeyEncapsulationService
            .DecapsulateAsync(key.Encapsulation, key.Metadata, CancellationToken.None)
            .ConfigureAwait(false);

        if (dek.Length != AesGcmConstants.KeyLengthInBytes)
        {
            throw new PropertyEncryptionException(
                $"The data encryption key '{key.Id}' must be {AesGcmConstants.KeyLengthInBytes} bytes for " +
                $"AES-256, but was {dek.Length}.");
        }

        _encryptionKeyCache.Set(profile, key.Id, dek);
        return dek;
    }
}
