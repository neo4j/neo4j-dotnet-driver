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

    private static readonly int AadProtocolMajor = BoltValueSerializationSchemeVersion.Latest.Major;
    private static readonly int AadProtocolMinor = BoltValueSerializationSchemeVersion.Latest.Minor;

    private readonly IPlaintextCodec _plaintextCodec;
    private readonly IPropertyTypeInspector _propertyTypeInspector;
    private readonly IAeadCipher _aeadCipher;
    private readonly IEncryptedValueBytesCodec _encryptedValueBytesCodec;
    private readonly IEnvelopeDataKeyProvider _envelopeDataKeyProvider;
    private readonly IBaselineCompatibilityGuard _baselineCompatibilityGuard;
    private readonly ICryptoRandomProvider _randomProvider;
    private readonly IEnvelopeMetadataExtractor _envelopeMetadataExtractor;
    private readonly IEnvelopeMetadataBuilder _envelopeMetadataBuilder;

    public EnvelopeEncryptionEngine(
        IPlaintextCodec plaintextCodec,
        IPropertyTypeInspector propertyTypeInspector,
        IAeadCipher aeadCipher,
        IEncryptedValueBytesCodec encryptedValueBytesCodec,
        IEnvelopeDataKeyProvider envelopeDataKeyProvider,
        IBaselineCompatibilityGuard baselineCompatibilityGuard,
        ICryptoRandomProvider randomProvider,
        IEnvelopeMetadataExtractor envelopeMetadataExtractor,
        IEnvelopeMetadataBuilder envelopeMetadataBuilder)
    {
        _plaintextCodec = plaintextCodec;
        _propertyTypeInspector = propertyTypeInspector;
        _aeadCipher = aeadCipher;
        _encryptedValueBytesCodec = encryptedValueBytesCodec;
        _envelopeDataKeyProvider = envelopeDataKeyProvider;
        _baselineCompatibilityGuard = baselineCompatibilityGuard;
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

        var (keyId, dataKey) = await _envelopeDataKeyProvider.GetDataKeyAsync(profile, keyRef, cancellationToken)
            .ConfigureAwait(false);

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

        if (_baselineCompatibilityGuard.IsUnsupportedBaselineType(structure, out var unsupported))
        {
            return unsupported;
        }

        var metadata = _envelopeMetadataExtractor.Extract(structure.Metadata);
        _baselineCompatibilityGuard.EnsureAadProtocolCompatibility(aad, metadata);

        var (_, dataKey) = await _envelopeDataKeyProvider
            .GetDataKeyAsync(profile, new KeyReference(metadata.KeyId, KeyReferenceType.Id), cancellationToken)
            .ConfigureAwait(false);

        var aadToUse = aad ?? metadata.Aad;
        var plaintext = _aeadCipher.Decrypt(dataKey, metadata.Iv, structure.CipherOutput, aadToUse);
        return _plaintextCodec.Deserialize(plaintext);
    }
}
