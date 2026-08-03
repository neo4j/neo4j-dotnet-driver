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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EnvelopeEncryptionEngine : IEncryptionEngine
{
    private const int IvLength = 12;
    private const int DataKeyLength = 32;

    // AAD is opaque bytes at this layer (no typed value to inspect) - fixed at the latest
    // baseline until the future API layer serializes typed AAD content itself.
    private static readonly int AadProtocolMajor = BoltValueSerializationSchemeVersion.Latest.Major;
    private static readonly int AadProtocolMinor = BoltValueSerializationSchemeVersion.Latest.Minor;

    private readonly IPlaintextCodec _plaintextCodec;
    private readonly IPropertyTypeInspector _propertyTypeInspector;
    private readonly IKeyDerivation _keyDerivation;
    private readonly IAeadCipher _aeadCipher;
    private readonly IEncryptedValueBytesCodec _encryptedValueBytesCodec;
    private readonly IAliasToKeyIdCache _aliasToKeyIdCache;
    private readonly IEncryptionKeyCache _encryptionKeyCache;
    private readonly ICryptoRandomProvider _randomProvider;
    private readonly IEnvelopeMetadataExtractor _envelopeMetadataExtractor;
    private readonly IEnvelopeMetadataBuilder _envelopeMetadataBuilder;

    public EnvelopeEncryptionEngine(
        IPlaintextCodec plaintextCodec,
        IPropertyTypeInspector propertyTypeInspector,
        IKeyDerivation keyDerivation,
        IAeadCipher aeadCipher,
        IEncryptedValueBytesCodec encryptedValueBytesCodec,
        IAliasToKeyIdCache aliasToKeyIdCache,
        IEncryptionKeyCache encryptionKeyCache,
        ICryptoRandomProvider randomProvider,
        IEnvelopeMetadataExtractor envelopeMetadataExtractor,
        IEnvelopeMetadataBuilder envelopeMetadataBuilder)
    {
        _plaintextCodec = plaintextCodec;
        _propertyTypeInspector = propertyTypeInspector;
        _keyDerivation = keyDerivation;
        _aeadCipher = aeadCipher;
        _encryptedValueBytesCodec = encryptedValueBytesCodec;
        _aliasToKeyIdCache = aliasToKeyIdCache;
        _encryptionKeyCache = encryptionKeyCache;
        _randomProvider = randomProvider;
        _envelopeMetadataExtractor = envelopeMetadataExtractor;
        _envelopeMetadataBuilder = envelopeMetadataBuilder;
    }

    public bool TryStartEncrypt(
        IInternalEncryptionProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out Task<byte[]>? encryptionTask)
    {
        if (profile is not IEnvelopeEncryptionProfile envelopeProfile)
        {
            encryptionTask = null;
            return false;
        }

        encryptionTask = EncryptAsync(envelopeProfile, value, keyRef, aad, cancellationToken);
        return true;
    }

    public bool TryStartDecrypt(
        IInternalEncryptionProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out Task<object>? decryptionTask)
    {
        if (profile is not IEnvelopeEncryptionProfile envelopeProfile)
        {
            decryptionTask = null;
            return false;
        }

        decryptionTask = DecryptAsync(envelopeProfile, encrypted, aad, cancellationToken);
        return true;
    }

    private async Task<byte[]> EncryptAsync(
        IEnvelopeEncryptionProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        var typeInfo = _propertyTypeInspector.GetPropertyTypeInfo(value);
        var plaintext = _plaintextCodec.Serialize(value);

        var (keyId, prefetchedKey) = await ResolveKeyIdAsync(profile, keyRef, cancellationToken).ConfigureAwait(false);
        var dek = await ResolveDataEncryptionKeyAsync(profile, keyId, prefetchedKey, cancellationToken)
            .ConfigureAwait(false);
        var dataKey = _keyDerivation.Derive(dek, DataKeyLength);

        var iv = new byte[IvLength];
        _randomProvider.Fill(iv);

        aad ??= [];
        var cipherResult = _aeadCipher.Encrypt(dataKey, iv, plaintext, aad);

        var envelopeMetadata = new EnvelopeMetadata(
            keyId,
            iv,
            aad,
            AadProtocolMajor,
            AadProtocolMinor,
            new Dictionary<string, object>());

        var metadata = _envelopeMetadataBuilder.Build(envelopeMetadata);
        var structure = new EncryptedStructure(
            profile.Name,
            cipherResult.Combined,
            typeInfo.Name,
            typeInfo.Baseline.Major,
            typeInfo.Baseline.Minor,
            metadata);

        return _encryptedValueBytesCodec.Encode(structure);
    }

    private async Task<object> DecryptAsync(
        IEnvelopeEncryptionProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        var structure = _encryptedValueBytesCodec.Decode(encrypted);

        if (IsUnsupportedBaselineType(structure, out var unsupported))
        {
            return unsupported;
        }

        var metadata = _envelopeMetadataExtractor.Extract(structure.Metadata);
        EnsureAadProtocolCompatibility(aad, metadata);

        var dek = await ResolveDataEncryptionKeyAsync(profile, metadata.KeyId, prefetchedKey: null, cancellationToken)
            .ConfigureAwait(false);
        var dataKey = _keyDerivation.Derive(dek, DataKeyLength);
        var aadToUse = aad ?? metadata.Aad;
        var plaintext = _aeadCipher.Decrypt(dataKey, metadata.Iv, structure.CipherOutput, aadToUse);
        return _plaintextCodec.Deserialize(plaintext);
    }

    private static bool IsUnsupportedBaselineType(
        EncryptedStructure structure,
        [NotNullWhen(true)] out UnsupportedType? unsupported)
    {
        var typeBaseline = new BoltValueSerializationSchemeVersion(
            structure.TypeSerializationSchemeMajor,
            structure.TypeSerializationSchemeMinor);

        if (typeBaseline > BoltValueSerializationSchemeVersion.Latest)
        {
            unsupported = new UnsupportedType(
                structure.TypeName,
                structure.TypeSerializationSchemeMajor,
                structure.TypeSerializationSchemeMinor,
                null);

            return true;
        }

        unsupported = null;
        return false;
    }

    private static void EnsureAadProtocolCompatibility(byte[]? aad, EnvelopeMetadata metadata)
    {
        if (aad == null)
        {
            return;
        }

        var aadBaseline = new BoltValueSerializationSchemeVersion(metadata.AadProtocolMajor, metadata.AadProtocolMinor);
        if (aadBaseline > BoltValueSerializationSchemeVersion.Latest)
        {
            throw new ClientException(
                $"Cannot reproduce AAD bytes: recorded AAD protocol version {aadBaseline} is newer than " +
                $"the maximum supported version {BoltValueSerializationSchemeVersion.Latest}.");
        }
    }

    // Id-typed refs resolve immediately; alias-typed refs check the alias cache first,
    // falling back to a repository lookup that primes the cache and hands back the
    // fetched row so the caller doesn't need a second round-trip to resolve the DEK.
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
