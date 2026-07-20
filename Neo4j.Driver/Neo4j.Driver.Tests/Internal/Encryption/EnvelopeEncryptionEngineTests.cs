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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Preview.Encryption;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeEncryptionEngineTests : UnitTestBase
{
    private const string ProfileName = "profile-a";

    private readonly Mock<IKeyEncapsulationService> _kes = new();
    private readonly Mock<IEncapsulatedKeyRepository> _repository = new();

    // SequentialRandom fills each buffer with 0,1,2,... so the generated IV is known.
    private static readonly byte[] Iv = Sequence(12);

    public EnvelopeEncryptionEngineTests()
    {
        Fixture.Inject<ICryptoRandomProvider>(new SequentialRandom());
    }

    private IEnvelopeProfile Profile()
    {
        var profile = new Mock<IEnvelopeProfile>();
        profile.SetupGet(p => p.Name).Returns(ProfileName);
        profile.SetupGet(p => p.KeyEncapsulationService).Returns(_kes.Object);
        profile.SetupGet(p => p.KeyRepository).Returns(_repository.Object);
        return profile.Object;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[] { 0x99 })]
    public async Task TryStartEncrypt_EncryptsAndEncodesTheStructure(byte[]? suppliedAad)
    {
        const long value = 5L;
        var plaintext = new byte[] { 0x10, 0x11 };
        var encapsulation = new byte[] { 0xBB };
        var options = new Dictionary<string, string> { ["iv"] = "wrap-iv" };
        var dek = Sequence(32, seed: 0x30);
        var dataKey = Sequence(32, seed: 0x40);
        var cipher = new CipherResult(new byte[] { 0xC0 }, new byte[] { 0xD0 });
        var encoded = new byte[] { 0xEE };
        var builtMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        byte[] expectedAad = suppliedAad ?? [];

        var key = new EncapsulatedKey("key-1", "main", encapsulation, options);

        Freeze<IPlaintextCodec>().Setup(s => s.Serialize(value)).Returns(plaintext);
        Freeze<IPropertyTypeInspector>()
            .Setup(n => n.GetPropertyTypeInfo(value))
            .Returns(new PropertyTypeInfo("INTEGER", new BoltProtocolVersion(1, 0)));
        _repository.Setup(r => r.FindAsync(
                new KeyReference("main", KeyReferenceType.Alias),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        byte[]? cacheMiss = null;
        Freeze<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(ProfileName, "key-1", out cacheMiss))
            .Returns(false);

        _kes.Setup(k => k.DecapsulateAsync(
                Matches(encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dek);

        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(dek), 32)).Returns(dataKey);
        Freeze<IAeadCipher>()
            .Setup(c => c.Encrypt(Matches(dataKey), Matches(Iv), Matches(plaintext), Matches(expectedAad)))
            .Returns(cipher);

        Freeze<IEnvelopeMetadataBuilder>()
            .Setup(b => b.Build(IsExpectedMetadata("key-1", expectedAad)))
            .Returns(builtMetadata);

        Freeze<IEncryptedStructureCodec>()
            .Setup(c => c.Encode(It.Is<EncryptedStructure>(s => IsExpectedStructure(s, cipher.Combined, builtMetadata))))
            .Returns(encoded);

        var subject = CreateSubject<EnvelopeEncryptionEngine>();
        var started = subject.TryStartEncrypt(
            Profile(),
            value,
            new KeyReference("main", KeyReferenceType.Alias),
            suppliedAad,
            TestContext.Current.CancellationToken,
            out var encryptedTask);

        started.Should().BeTrue();
        var result = await encryptedTask!;

        result.Should().Equal(encoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[] { 0x99 })]
    public async Task TryStartDecrypt_ResolvesKeyByIdAndUsesSuppliedAadElsePersisted(byte[]? suppliedAad)
    {
        var encrypted = new byte[] { 0xEE };
        var cipherOutput = new byte[] { 0xC0, 0xD0 };
        var persistedAad = new byte[] { 0xAA };
        var structureMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        var structure = new EncryptedStructure(ProfileName, cipherOutput, "INTEGER", 6, 0, structureMetadata);
        var envelopeMetadata = new EnvelopeMetadata(
            "key-1",
            Iv,
            persistedAad,
            6,
            0,
            new Dictionary<string, object>());

        var encapsulation = new byte[] { 0xBB };
        var keyMetadata = new Dictionary<string, string> { ["iv"] = "wrap-iv" };
        var dek = Sequence(32, seed: 0x30);
        var dataKey = Sequence(32, seed: 0x40);
        var plaintext = new byte[] { 0x10, 0x11 };
        const long value = 5L;

        var key = new EncapsulatedKey("key-1", "main", encapsulation, keyMetadata);

        Freeze<IEncryptedStructureCodec>().Setup(c => c.Decode(Matches(encrypted))).Returns(structure);
        Freeze<IEnvelopeMetadataExtractor>().Setup(e => e.Extract(structureMetadata)).Returns(envelopeMetadata);

        _repository.Setup(r => r.FindAsync(
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        byte[]? cacheMiss = null;
        Freeze<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(ProfileName, "key-1", out cacheMiss))
            .Returns(false);

        _kes.Setup(k => k.DecapsulateAsync(
                Matches(encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dek);

        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(dek), 32)).Returns(dataKey);

        var expectedAad = suppliedAad ?? persistedAad;
        Freeze<IAeadCipher>()
            .Setup(c => c.Decrypt(Matches(dataKey), Matches(Iv), Matches(cipherOutput), Matches(expectedAad)))
            .Returns(plaintext);

        Freeze<IPlaintextCodec>().Setup(d => d.Deserialize(Matches(plaintext))).Returns(value);

        var subject = CreateSubject<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            Profile(),
            encrypted,
            suppliedAad,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(7, 0, "7.0")]
    [InlineData(6, 2, "6.2")]
    public async Task TryStartDecrypt_TypeBaselineNewerThanLatestKnown_ReturnsUnsupportedType(
        int typeProtocolMajor,
        int typeProtocolMinor,
        string expectedMinimumVersion)
    {
        var encrypted = new byte[] { 0xEE };
        var structure = new EncryptedStructure(
            ProfileName,
            [0xC0, 0xD0],
            "VECTOR",
            typeProtocolMajor,
            typeProtocolMinor,
            new Dictionary<string, object>());

        Freeze<IEncryptedStructureCodec>().Setup(c => c.Decode(Matches(encrypted))).Returns(structure);

        var subject = CreateSubject<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            Profile(),
            encrypted,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        var unsupported = result.Should().BeOfType<UnsupportedType>().Subject;
        unsupported.Name.Should().Be("VECTOR");
        unsupported.MinimumProtocolVersion.Should().Be(expectedMinimumVersion);
    }

    [Fact]
    public async Task TryStartDecrypt_NoSuppliedAadWithAadBaselineNewerThanLatestKnown_UsesPersistedAadWithoutThrowing()
    {
        var encrypted = new byte[] { 0xEE };
        var cipherOutput = new byte[] { 0xC0, 0xD0 };
        var persistedAad = new byte[] { 0xAA };
        var structureMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        var structure = new EncryptedStructure(ProfileName, cipherOutput, "INTEGER", 1, 0, structureMetadata);
        var envelopeMetadata = new EnvelopeMetadata(
            "key-1",
            Iv,
            persistedAad,
            AadProtocolMajor: 7,
            AadProtocolMinor: 0,
            new Dictionary<string, object>());

        var encapsulation = new byte[] { 0xBB };
        var keyMetadata = new Dictionary<string, string> { ["iv"] = "wrap-iv" };
        var dek = Sequence(32, seed: 0x30);
        var dataKey = Sequence(32, seed: 0x40);
        var plaintext = new byte[] { 0x10, 0x11 };
        const long value = 5L;

        var key = new EncapsulatedKey("key-1", "main", encapsulation, keyMetadata);

        Freeze<IEncryptedStructureCodec>().Setup(c => c.Decode(Matches(encrypted))).Returns(structure);
        Freeze<IEnvelopeMetadataExtractor>().Setup(e => e.Extract(structureMetadata)).Returns(envelopeMetadata);

        _repository.Setup(r => r.FindAsync(
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        byte[]? cacheMiss = null;
        Freeze<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(ProfileName, "key-1", out cacheMiss))
            .Returns(false);

        _kes.Setup(k => k.DecapsulateAsync(
                Matches(encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dek);

        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(dek), 32)).Returns(dataKey);
        Freeze<IAeadCipher>()
            .Setup(c => c.Decrypt(Matches(dataKey), Matches(Iv), Matches(cipherOutput), Matches(persistedAad)))
            .Returns(plaintext);

        Freeze<IPlaintextCodec>().Setup(d => d.Deserialize(Matches(plaintext))).Returns(value);

        var subject = CreateSubject<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            Profile(),
            encrypted,
            aad: null,
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var result = await decryptedTask!;

        result.Should().Be(value);
    }

    [Fact]
    public async Task TryStartDecrypt_SuppliedAadWithAadBaselineNewerThanLatestKnown_Throws()
    {
        var encrypted = new byte[] { 0xEE };
        var structureMetadata = new Dictionary<string, object> { ["key_id"] = "key-1" };
        var structure = new EncryptedStructure(ProfileName, [0xC0, 0xD0], "INTEGER", 1, 0, structureMetadata);
        var envelopeMetadata = new EnvelopeMetadata(
            "key-1",
            Iv,
            [0xAA],
            AadProtocolMajor: 7,
            AadProtocolMinor: 0,
            new Dictionary<string, object>());

        Freeze<IEncryptedStructureCodec>().Setup(c => c.Decode(Matches(encrypted))).Returns(structure);
        Freeze<IEnvelopeMetadataExtractor>().Setup(e => e.Extract(structureMetadata)).Returns(envelopeMetadata);

        var subject = CreateSubject<EnvelopeEncryptionEngine>();
        var started = subject.TryStartDecrypt(
            Profile(),
            encrypted,
            aad: [0x99],
            TestContext.Current.CancellationToken,
            out var decryptedTask);

        started.Should().BeTrue();
        var act = async () => await decryptedTask!;

        await act.Should().ThrowAsync<ClientException>();
    }

    [Fact]
    public void TryStartEncrypt_WithNonEnvelopeProfile_ReturnsFalse()
    {
        var subject = CreateSubject<EnvelopeEncryptionEngine>();

        var result = subject.TryStartEncrypt(
            Mock.Of<IEncryptionProfile>(),
            5L,
            keyRef: new KeyReference("main", KeyReferenceType.Alias),
            aad: null,
            TestContext.Current.CancellationToken,
            out var encrypted);

        result.Should().BeFalse();
        encrypted.Should().BeNull();
    }

    [Fact]
    public void TryStartDecrypt_WithNonEnvelopeProfile_ReturnsFalse()
    {
        var subject = CreateSubject<EnvelopeEncryptionEngine>();

        var result = subject.TryStartDecrypt(
            Mock.Of<IEncryptionProfile>(),
            [0xEE],
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
        return s.ProfileName == ProfileName &&
            s.TypeName == "INTEGER" &&
            s.TypeProtocolMajor == 1 &&
            s.TypeProtocolMinor == 0 &&
            s.CipherOutput.SequenceEqual(expectedCipherOutput) &&
            ReferenceEquals(s.Metadata, expectedMetadata);
    }

    private static EnvelopeMetadata IsExpectedMetadata(string keyId, byte[] expectedAad)
    {
        return It.Is<EnvelopeMetadata>(m =>
            m.KeyId == keyId &&
            m.Iv.SequenceEqual(Iv) &&
            m.Aad.SequenceEqual(expectedAad) &&
            m.AadProtocolMajor == 6 &&
            m.AadProtocolMinor == 1);
    }
}
