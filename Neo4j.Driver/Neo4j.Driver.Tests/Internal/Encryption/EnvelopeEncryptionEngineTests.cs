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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeEncryptionEngineTests
{
    private const string ProfileName = "profile-a";

    private static readonly byte[] Iv = Sequence(12);

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EnvelopeEncryptionEngine>();

    public EnvelopeEncryptionEngineTests()
    {
        _autoMocker.Use(Mock.Of<IIvProvider>(p => p.GetIv() == Iv));
    }

    private static IEnvelopeEncryptionProfile Profile()
    {
        return Mock.Of<IEnvelopeEncryptionProfile>(p => p.Name == ProfileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[] { 0x99 })]
    public async Task TryStartEncrypt_EncryptsAndEncodesTheStructure(byte[]? suppliedAad)
    {
        const long value = 5L;
        var plaintext = new byte[] { 0x10, 0x11 };
        var dataKey = Sequence(32, seed: 0x40);
        var cipher = new CipherResult([0xC0, 0xD0], TagLength: 1);
        var encoded = new byte[] { 0xEE };
        var builtMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        byte[] expectedAad = suppliedAad ?? [];
        var profile = Profile();

        _autoMocker.GetMock<IPlaintextCodec>().Setup(s => s.Serialize(value)).Returns(plaintext);
        _autoMocker.GetMock<IPropertyTypeInspector>()
            .Setup(n => n.GetPropertyTypeInfo(value))
            .Returns(new PropertyTypeInfo("INTEGER", new BoltValueSerializationSchemeVersion(1, 0)));

        _autoMocker.GetMock<IEnvelopeDataKeyProvider>()
            .Setup(p => p.GetDataKeyAsync(
                profile,
                new KeyReference("main", KeyReferenceType.Alias),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataKeyResult("key-1", dataKey));

        _autoMocker.GetMock<IAeadCipher>()
            .Setup(c => c.Encrypt(Matches(dataKey), Matches(Iv), Matches(plaintext), Matches(expectedAad)))
            .Returns(cipher);

        _autoMocker.GetMock<IEnvelopeMetadataBuilder>()
            .Setup(b => b.Build(IsExpectedMetadata("key-1", expectedAad)))
            .Returns(builtMetadata);

        _autoMocker.GetMock<IEncryptedValueBytesCodec>()
            .Setup(c => c.Encode(It.Is<EncryptedStructure>(s => IsExpectedStructure(s, cipher.CipherOutput, builtMetadata))))
            .Returns(encoded);

        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        var started = subject.TryStartEncrypt(
            profile,
            value,
            new KeyReference("main", KeyReferenceType.Alias),
            suppliedAad,
            iv: null,
            TestContext.Current.CancellationToken,
            out var encryptedTask);

        started.Should().BeTrue();
        var result = await encryptedTask!;

        result.Should().Equal(encoded);
    }

    [Fact]
    public async Task TryStartEncrypt_WithASuppliedIv_UsesItInsteadOfTheProviderIv()
    {
        const long value = 5L;
        var plaintext = new byte[] { 0x10, 0x11 };
        var dataKey = Sequence(32, seed: 0x40);
        var suppliedIv = Sequence(12, seed: 0x70);
        var cipher = new CipherResult([0xC0, 0xD0], TagLength: 1);
        var profile = Profile();
        byte[]? ivGivenToCipher = null;

        _autoMocker.GetMock<IPlaintextCodec>().Setup(s => s.Serialize(value)).Returns(plaintext);
        _autoMocker.GetMock<IPropertyTypeInspector>()
            .Setup(n => n.GetPropertyTypeInfo(value))
            .Returns(new PropertyTypeInfo("INTEGER", new BoltValueSerializationSchemeVersion(1, 0)));

        _autoMocker.GetMock<IEnvelopeDataKeyProvider>()
            .Setup(p => p.GetDataKeyAsync(
                profile,
                new KeyReference("main", KeyReferenceType.Alias),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataKeyResult("key-1", dataKey));

        _autoMocker.GetMock<IAeadCipher>()
            .Setup(c => c.Encrypt(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Callback<byte[], byte[], byte[], byte[]>((_, iv, _, _) => ivGivenToCipher = iv)
            .Returns(cipher);

        _autoMocker.GetMock<IEnvelopeMetadataBuilder>()
            .Setup(b => b.Build(It.IsAny<EnvelopeMetadata>()))
            .Returns(new Dictionary<string, object> { ["key_id"] = "key-1" });

        _autoMocker.GetMock<IEncryptedValueBytesCodec>()
            .Setup(c => c.Encode(It.IsAny<EncryptedStructure>()))
            .Returns([0xEE]);

        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        var started = subject.TryStartEncrypt(
            profile,
            value,
            new KeyReference("main", KeyReferenceType.Alias),
            aad: null,
            iv: suppliedIv,
            TestContext.Current.CancellationToken,
            out var encryptedTask);

        started.Should().BeTrue();
        await encryptedTask!;

        ivGivenToCipher.Should().Equal(suppliedIv);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[] { 0x99 })]
    public async Task TryStartDecrypt_ResolvesKeyByIdAndUsesSuppliedAadElsePersisted(byte[]? suppliedAad)
    {
        var cipherOutput = new byte[] { 0xC0, 0xD0 };
        var persistedAad = new byte[] { 0xAA };
        var structureMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        var structure = new EncryptedStructure(
            "ENVELOPE",
            1,
            ProfileName,
            cipherOutput,
            "INTEGER",
            1,
            0,
            structureMetadata);
        var envelopeMetadata = new EnvelopeMetadata(
            "key-1",
            Iv,
            persistedAad,
            1,
            0);

        var dataKey = Sequence(32, seed: 0x40);
        var plaintext = new byte[] { 0x10, 0x11 };
        const long value = 5L;
        var profile = Profile();

        _autoMocker.GetMock<IEnvelopeMetadataExtractor>().Setup(e => e.Extract(structureMetadata)).Returns(envelopeMetadata);

        _autoMocker.GetMock<IEnvelopeDataKeyProvider>()
            .Setup(p => p.GetDataKeyAsync(
                profile,
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataKeyResult("key-1", dataKey));

        var expectedAad = suppliedAad ?? persistedAad;
        _autoMocker.GetMock<IAeadCipher>()
            .Setup(c => c.Decrypt(Matches(dataKey), Matches(Iv), Matches(cipherOutput), Matches(expectedAad)))
            .Returns(plaintext);

        _autoMocker.GetMock<IPlaintextCodec>().Setup(d => d.Deserialize(Matches(plaintext))).Returns(value);

        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            profile,
            structure,
            suppliedAad,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        result.Should().Be(value);
    }

    [Fact]
    public async Task TryStartDecrypt_WithAProfileTypeTheDriverCannotHandle_ThrowsNamingThatType()
    {
        var structure = new EncryptedStructure(
            "STATIC_KEYS",
            1,
            ProfileName,
            [0xC0, 0xD0],
            "INTEGER",
            1,
            0,
            new Dictionary<string, object>());


        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        subject.TryStartDecrypt(
            Profile(),
            structure,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        var act = async () => await decryptedTask!;

        await act.Should()
            .ThrowAsync<UnsupportedEncryptionProfileTypeException>()
            .WithMessage("*STATIC_KEYS*");
    }

    [Fact]
    public async Task TryStartDecrypt_WithAProfileVersionTheDriverCannotHandle_ThrowsNamingThatVersion()
    {
        var structure = new EncryptedStructure(
            "ENVELOPE",
            2,
            ProfileName,
            [0xC0, 0xD0],
            "INTEGER",
            1,
            0,
            new Dictionary<string, object>());


        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        subject.TryStartDecrypt(
            Profile(),
            structure,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        var act = async () => await decryptedTask!;

        await act.Should()
            .ThrowAsync<UnsupportedEncryptionProfileVersionException>()
            .WithMessage("*2*");
    }

    [Fact]
    public async Task TryStartDecrypt_GuardReportsUnsupportedBaselineType_ReturnsItWithoutDecrypting()
    {
        var structure = new EncryptedStructure(
            "ENVELOPE",
            1,
            ProfileName,
            [0xC0, 0xD0],
            "VECTOR",
            7,
            0,
            new Dictionary<string, object>());


        var unsupported = new UnsupportedType("VECTOR", 7, 0, null);
        UnsupportedType? guardResult = unsupported;
        _autoMocker.GetMock<IBaselineCompatibilityGuard>()
            .Setup(g => g.IsUnsupportedBaselineType(structure, out guardResult))
            .Returns(true);

        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            Profile(),
            structure,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        result.Should().BeSameAs(unsupported);
    }

    [Fact]
    public async Task TryStartDecrypt_PersistedAadOnly_PassesTheRawNullAadToTheGuardNotTheResolvedAad()
    {
        var cipherOutput = new byte[] { 0xC0, 0xD0 };
        var persistedAad = new byte[] { 0xAA };
        var structureMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        var structure = new EncryptedStructure(
            "ENVELOPE",
            1,
            ProfileName,
            cipherOutput,
            "INTEGER",
            1,
            0,
            structureMetadata);
        var envelopeMetadata = new EnvelopeMetadata(
            "key-1",
            Iv,
            persistedAad,
            1,
            0);

        var dataKey = Sequence(32, seed: 0x40);
        var plaintext = new byte[] { 0x10, 0x11 };
        const long value = 5L;
        var profile = Profile();

        _autoMocker.GetMock<IEnvelopeMetadataExtractor>().Setup(e => e.Extract(structureMetadata)).Returns(envelopeMetadata);

        _autoMocker.GetMock<IBaselineCompatibilityGuard>()
            .Setup(g => g.EnsureAadEncodingSchemeCompatibility(
                It.Is<byte[]?>(a => a != null),
                It.IsAny<EnvelopeMetadata>()))
            .Throws(new ClientException("engine passed a non-null AAD to the guard"));

        _autoMocker.GetMock<IEnvelopeDataKeyProvider>()
            .Setup(p => p.GetDataKeyAsync(
                profile,
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataKeyResult("key-1", dataKey));

        _autoMocker.GetMock<IAeadCipher>()
            .Setup(c => c.Decrypt(Matches(dataKey), Matches(Iv), Matches(cipherOutput), Matches(persistedAad)))
            .Returns(plaintext);

        _autoMocker.GetMock<IPlaintextCodec>().Setup(d => d.Deserialize(Matches(plaintext))).Returns(value);

        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            profile,
            structure,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        result.Should().Be(value);
    }

    [Fact]
    public void TryStartEncrypt_WithNonEnvelopeProfile_ReturnsFalse()
    {
        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();

        var result = subject.TryStartEncrypt(
            Mock.Of<IInternalEncryptionProfile>(),
            5L,
            keyRef: new KeyReference("main", KeyReferenceType.Alias),
            aad: null,
            iv: null,
            TestContext.Current.CancellationToken,
            out var encrypted);

        result.Should().BeFalse();
        encrypted.Should().BeNull();
    }

    [Fact]
    public void TryStartDecrypt_WithNonEnvelopeProfile_ReturnsFalse()
    {
        var subject = _autoMocker.CreateInstance<EnvelopeEncryptionEngine>();

        var result = subject.TryStartDecrypt(
            Mock.Of<IInternalEncryptionProfile>(),
            new EncryptedStructure("ENVELOPE", 1, ProfileName, [], "INTEGER", 1, 0, new Dictionary<string, object>()),
            aad: null,
            TestContext.Current.CancellationToken,
            out var decrypted);

        result.Should().BeFalse();
        decrypted.Should().BeNull();
    }

    private static bool IsExpectedStructure(
        EncryptedStructure s,
        byte[] expectedCipherOutput,
        IDictionary<string, object> expectedMetadata)
    {
        return s.ProfileType == "ENVELOPE" &&
            s.ProfileVersion == 1 &&
            s.ProfileName == ProfileName &&
            s.TypeName == "INTEGER" &&
            s.TypeSerializationSchemeMajor == 1 &&
            s.TypeSerializationSchemeMinor == 0 &&
            s.CipherOutput.SequenceEqual(expectedCipherOutput) &&
            ReferenceEquals(s.Metadata, expectedMetadata);
    }

    private static EnvelopeMetadata IsExpectedMetadata(string keyId, byte[] expectedAad)
    {
        return It.Is<EnvelopeMetadata>(m =>
            m.KeyId == keyId &&
            m.Iv.SequenceEqual(Iv) &&
            m.Aad.SequenceEqual(expectedAad) &&
            m.AadEncodingSchemeMajor == 1 &&
            m.AadEncodingSchemeMinor == 0);
    }
}
