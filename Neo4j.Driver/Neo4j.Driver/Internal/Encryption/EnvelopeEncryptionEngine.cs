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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Preview.Encryption;

namespace Neo4j.Driver.Internal.Encryption;

[DriverAutoRegister(singleton: true)]
internal class EnvelopeEncryptionEngine : IEncryptionEngine
{
    private const int IvLength = 12;
    private const int DataKeyLength = 32;
    private static readonly int ProtocolMajor = BoltProtocolVersion.V6_0.MajorVersion;
    private static readonly int ProtocolMinor = BoltProtocolVersion.V6_0.MinorVersion;

    private readonly IPlaintextCodec _plaintextCodec;
    private readonly IPropertyTypeNamer _propertyTypeNamer;
    private readonly IKeyDerivation _keyDerivation;
    private readonly IAeadCipher _aeadCipher;
    private readonly IEncryptedStructureCodec _encryptedStructureCodec;
    private readonly IEncryptionKeyCache _encryptionKeyCache;
    private readonly ICryptoRandomProvider _randomProvider;
    private readonly IEnvelopeMetadataExtractor _envelopeMetadataExtractor;
    private readonly IEnvelopeMetadataBuilder _envelopeMetadataBuilder;

    public EnvelopeEncryptionEngine(
        IPlaintextCodec plaintextCodec,
        IPropertyTypeNamer propertyTypeNamer,
        IKeyDerivation keyDerivation,
        IAeadCipher aeadCipher,
        IEncryptedStructureCodec encryptedStructureCodec,
        IEncryptionKeyCache encryptionKeyCache,
        ICryptoRandomProvider randomProvider,
        IEnvelopeMetadataExtractor envelopeMetadataExtractor,
        IEnvelopeMetadataBuilder envelopeMetadataBuilder)
    {
        _plaintextCodec = plaintextCodec;
        _propertyTypeNamer = propertyTypeNamer;
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
        KeyReference? keyRef,
        byte[]? aad,
        CancellationToken cancellationToken,
        out Task<byte[]>? encryptionTask)
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
        out Task<object>? decryptionTask)
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
        KeyReference? keyRef,
        byte[]? aad,
        CancellationToken cancellationToken)
    {
        var typeName = _propertyTypeNamer.GetValidTypeName(value);
        var plaintext = _plaintextCodec.Serialize(value);

        var key = await profile.KeyRepository.FindAsync(keyRef ?? profile.DefaultKeyReference, cancellationToken)
            .ConfigureAwait(false);

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
            ProtocolMajor,
            ProtocolMinor,
            new Dictionary<string, object>());

        var metadata = _envelopeMetadataBuilder.Build(envelopeMetadata);
        var structure = new EncryptedStructure(
            profile.Name,
            cipherResult.Combined,
            typeName,
            ProtocolMajor,
            ProtocolMinor,
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
        var metadata = _envelopeMetadataExtractor.Extract(structure.Metadata);
        var key = await profile.KeyRepository
            .FindAsync(new KeyReference(metadata.KeyId, KeyReferenceType.Id), cancellationToken)
            .ConfigureAwait(false);

        var dek = await ResolveDataEncryptionKeyAsync(profile, key, cancellationToken).ConfigureAwait(false);
        var dataKey = _keyDerivation.Derive(dek, DataKeyLength);
        var aadToUse = aad ?? metadata.Aad;
        var plaintext = _aeadCipher.Decrypt(dataKey, metadata.Iv, structure.CipherOutput, aadToUse);
        return _plaintextCodec.Deserialize(plaintext);
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
