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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EnvelopeEncryptionEngine : IEncryptionEngine
{
    private const int IvLength = 12;
    private const int DataKeyLength = 32;

    // AAD is opaque bytes at this layer (no typed value to inspect) - fixed at the latest
    // baseline until the future API layer serializes typed AAD content itself.
    private static readonly int AadProtocolMajor = BoltProtocolVersion.V6_1.MajorVersion;
    private static readonly int AadProtocolMinor = BoltProtocolVersion.V6_1.MinorVersion;

    private readonly IPlaintextCodec _plaintextCodec;
    private readonly IPropertyTypeInspector _propertyTypeInspector;
    private readonly IKeyDerivation _keyDerivation;
    private readonly IAeadCipher _aeadCipher;
    private readonly IEncryptedStructureCodec _encryptedStructureCodec;
    private readonly IEncryptionKeyCache _encryptionKeyCache;
    private readonly ICryptoRandomProvider _randomProvider;
    private readonly IEnvelopeMetadataExtractor _envelopeMetadataExtractor;
    private readonly IEnvelopeMetadataBuilder _envelopeMetadataBuilder;

    public EnvelopeEncryptionEngine(
        IPlaintextCodec plaintextCodec,
        IPropertyTypeInspector propertyTypeInspector,
        IKeyDerivation keyDerivation,
        IAeadCipher aeadCipher,
        IEncryptedStructureCodec encryptedStructureCodec,
        IEncryptionKeyCache encryptionKeyCache,
        ICryptoRandomProvider randomProvider,
        IEnvelopeMetadataExtractor envelopeMetadataExtractor,
        IEnvelopeMetadataBuilder envelopeMetadataBuilder)
    {
        _plaintextCodec = plaintextCodec;
        _propertyTypeInspector = propertyTypeInspector;
        _keyDerivation = keyDerivation;
        _aeadCipher = aeadCipher;
        _encryptedStructureCodec = encryptedStructureCodec;
        _encryptionKeyCache = encryptionKeyCache;
        _randomProvider = randomProvider;
        _envelopeMetadataExtractor = envelopeMetadataExtractor;
        _envelopeMetadataBuilder = envelopeMetadataBuilder;
    }

    public bool TryStartEncrypt(
        IEncryptionProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out Task<byte[]>? encryptionTask)
    {
        if (profile is not IEnvelopeProfile envelopeProfile)
        {
            encryptionTask = null;
            return false;
        }

        encryptionTask = EncryptAsync(envelopeProfile, value, keyRef, aad, cancellationToken);
        return true;
    }

    public bool TryStartDecrypt(
        IEncryptionProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out Task<object>? decryptionTask)
    {
        if (profile is not IEnvelopeProfile envelopeProfile)
        {
            decryptionTask = null;
            return false;
        }

        decryptionTask = DecryptAsync(envelopeProfile, encrypted, aad, cancellationToken);
        return true;
    }

    private async Task<byte[]> EncryptAsync(
        IEnvelopeProfile profile,
        object value,
        KeyReference keyRef,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        var typeInfo = _propertyTypeInspector.GetPropertyTypeInfo(value);
        var plaintext = _plaintextCodec.Serialize(value);

        var key = await profile.KeyRepository.FindAsync(keyRef, cancellationToken).ConfigureAwait(false);

        var dek = await ResolveDataEncryptionKeyAsync(profile, key, cancellationToken).ConfigureAwait(false);
        var dataKey = _keyDerivation.Derive(dek, DataKeyLength);

        var iv = new byte[IvLength];
        _randomProvider.Fill(iv);

        aad ??= [];
        var cipherResult = _aeadCipher.Encrypt(dataKey, iv, plaintext, aad);

        var envelopeMetadata = new EnvelopeMetadata(
            key.Id,
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
            typeInfo.Baseline.MajorVersion,
            typeInfo.Baseline.MinorVersion,
            metadata);

        return _encryptedStructureCodec.Encode(structure);
    }

    private async Task<object> DecryptAsync(
        IEnvelopeProfile profile,
        byte[] encrypted,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        var structure = _encryptedStructureCodec.Decode(encrypted);

        if (IsUnsupportedBaselineType(structure, out var unsupported))
        {
            return unsupported;
        }

        var metadata = _envelopeMetadataExtractor.Extract(structure.Metadata);
        EnsureAadProtocolCompatibility(aad, metadata);
        var key = await profile.KeyRepository
            .FindAsync(new KeyReference(metadata.KeyId, KeyReferenceType.Id), cancellationToken)
            .ConfigureAwait(false);

        var dek = await ResolveDataEncryptionKeyAsync(profile, key, cancellationToken).ConfigureAwait(false);
        var dataKey = _keyDerivation.Derive(dek, DataKeyLength);
        var aadToUse = aad ?? metadata.Aad;
        var plaintext = _aeadCipher.Decrypt(dataKey, metadata.Iv, structure.CipherOutput, aadToUse);
        return _plaintextCodec.Deserialize(plaintext);
    }

    private static bool IsUnsupportedBaselineType(
        EncryptedStructure structure,
        [NotNullWhen(true)] out UnsupportedType? unsupported)
    {
        var typeBaseline = new BoltProtocolVersion(structure.TypeProtocolMajor, structure.TypeProtocolMinor);
        if (typeBaseline > BoltProtocolVersion.LatestVersion)
        {
            unsupported = new UnsupportedType(
                structure.TypeName,
                structure.TypeProtocolMajor,
                structure.TypeProtocolMinor,
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

        var aadBaseline = new BoltProtocolVersion(metadata.AadProtocolMajor, metadata.AadProtocolMinor);
        if (aadBaseline > BoltProtocolVersion.LatestVersion)
        {
            throw new ClientException(
                $"Cannot reproduce AAD bytes: recorded AAD protocol version {aadBaseline} is newer than " +
                $"the maximum supported version {BoltProtocolVersion.LatestVersion}.");
        }
    }

    private async Task<byte[]> ResolveDataEncryptionKeyAsync(
        IEnvelopeProfile profile,
        EncapsulatedKey key,
        CancellationToken cancellationToken = default)
    {
        if (_encryptionKeyCache.TryGet(profile.Name, key.Id, out var cached))
        {
            return cached;
        }

        var dek = await profile.KeyEncapsulationService
            .DecapsulateAsync(key.Encapsulation, key.Metadata, cancellationToken)
            .ConfigureAwait(false);

        _encryptionKeyCache.Set(profile.Name, key.Id, dek);
        return dek;
    }
}
