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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Public.Preview.Encryption;

public class PropertyEncryptionFullStackTests : IAsyncLifetime
{
    private static readonly byte[] Kek = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

    private IDriver _driver = null!;
    private IPropertyEncryption _propertyEncryption = null!;
    private string _keyId = null!;

    public async ValueTask InitializeAsync()
    {
        _driver = GraphDatabase.Driver(
            "bolt://localhost",
            builder => builder.WithPropertyEncryptionProfiles([EnvelopeProfile("test-profile")]));

        _propertyEncryption = _driver.PropertyEncryption();
        var key = await _propertyEncryption.KeyManager()
            .CreateAsync("main", cancellationToken: TestContext.Current.CancellationToken);
        _keyId = key.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }

    private static IPropertyEncryptionProfile EnvelopeProfile(string name)
    {
        var kes = KeyEncapsulationServices.Local(Kek);

        return PropertyEncryptionProfile
            .EnvelopeBuilder(name, kes, new InMemoryEncapsulatedKeyRepository(new KeyIdGenerator()))
            .Build();
    }

    public static TheoryData<object> SupportedValues()
    {
        return new()
        {
            true,
            false,
            42L,
            -1L,
            3.25,
            "hello",
            "",
            new byte[] { 0x01, 0x02, 0x03 },
            new List<object> { 1L, 2L, 3L },
            new List<object> { "a", "b" },
            new List<object>()
        };
    }

    [Theory]
    [MemberData(nameof(SupportedValues))]
    public async Task EncryptThenDecrypt_ByKeyAlias_RoundTripsTheValue(object value)
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue(value)
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().BeEquivalentTo(value);
    }

    [Fact]
    public async Task EncryptThenDecrypt_AnInt_DecryptsAsALong()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue(42)
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().Be(42L);
    }

    [Fact]
    public async Task EncryptThenDecrypt_ByKeyId_RoundTripsTheValue()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("by-id")
            .UsingKeyId(_keyId)
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().Be("by-id");
    }

    [Fact]
    public async Task EncryptThenDecrypt_WithExplicitAad_RoundTripsWhenTheSameAadIsSupplied()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("aad-bound")
            .WithAad("row-42")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithAad("row-42")
            .DecryptAsync(token);

        decrypted.Should().Be("aad-bound");
    }

    [Fact]
    public async Task EncryptThenDecrypt_WithExplicitAad_AlsoRoundTripsViaThePersistedAad()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("aad-bound")
            .WithAad("row-42")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().Be("aad-bound");
    }

    [Fact]
    public async Task EncryptThenDecrypt_WithANullValue_RoundTrips()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue(null)
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().BeNull();
    }

    [Fact]
    public async Task EncryptThenDecrypt_WithANonStringAad_RoundTripsWhenTheSameAadIsSupplied()
    {
        var token = TestContext.Current.CancellationToken;
        var aad = new LocalDate(2026, 10, 6);

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("date-bound")
            .WithAad(aad)
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithAad(aad)
            .DecryptAsync(token);

        decrypted.Should().Be("date-bound");
    }

    [Fact]
    public void Encrypt_WithAnAadTypeTheAdrDoesNotAllow_ThrowsImmediately()
    {
        var keyStep = _propertyEncryption.EncryptRequest().FromValue("hello");

        var act = () => keyStep.WithAad(1.5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_WithAnAadTypeTheAdrDoesNotAllow_ThrowsImmediately()
    {
        var aadStep = _propertyEncryption.DecryptRequest().FromValue([0x01]);

        var act = () => aadStep.WithAad(new List<object> { "row-42" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Decrypt_WithTheWrongAad_Throws()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("aad-bound")
            .WithAad("row-42")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithAad("row-999")
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Theory]
    [InlineData("01")]
    [InlineData("01B865")]
    [InlineData("01B86588454E56454C4F5045")]
    [InlineData("01B86588454E56454C4F5045CB0000000100000001")]
    public async Task Decrypt_WithMalformedBytes_Throws(string hex)
    {
        var token = TestContext.Current.CancellationToken;
        var malformed = Convert.FromHexString(hex);

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(malformed)
            .WithPersistedAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Fact]
    public async Task Decrypt_WithBytesTrailingTheStructure_Throws()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = await EncryptByAliasAsync("value", token);
        byte[] withTrailingByte = [..encrypted, 0x01];

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(withTrailingByte)
            .WithPersistedAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<ProtocolException>();
    }

    [Fact]
    public async Task Decrypt_WithAStructureOfNineFields_Throws()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = await EncryptByAliasAsync("value", token);
        byte[] withNinthField = [..encrypted, 0x01];
        withNinthField[1] = 0xB9;

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(withNinthField)
            .WithPersistedAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<ProtocolException>();
    }

    private Task<byte[]> EncryptByAliasAsync(object value, CancellationToken token)
    {
        return _propertyEncryption.EncryptRequest()
            .FromValue(value)
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);
    }

    [Fact]
    public async Task Decrypt_WithTamperedCiphertext_Throws()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("tamper-me")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var tampered = TamperWithCipherOutput(encrypted);

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(tampered)
            .WithPersistedAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    private static byte[] TamperWithCipherOutput(byte[] encrypted)
    {
        return Rewrite(
            encrypted,
            structure =>
            {
                structure.CipherOutput[0] ^= 0xFF;
                return structure;
            });
    }

    private static byte[] Rewrite(byte[] encrypted, Func<EncryptedStructure, EncryptedStructure> change)
    {
        var codec = new EncryptedValueBytesCodec(
            new EncryptedStructureCodec(
                new MessageFormatFactory(TestDriverContext.MockContext),
                new PackStreamMemorySerializer(new PackStreamReaderWriterFactory())));

        return codec.Encode(change(codec.Decode(encrypted)));
    }

    private static byte[] WithANewerTypeBaseline(byte[] encrypted)
    {
        return Rewrite(encrypted, structure => structure with { TypeSerializationSchemeMajor = 7 });
    }

    [Fact]
    public async Task Decrypt_WithANewerTypeBaselineAndTheRightAad_ReturnsUnsupportedType()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("future-value")
            .WithAad("row-42")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(WithANewerTypeBaseline(encrypted))
            .WithAad("row-42")
            .DecryptAsync(token);

        decrypted.Should().BeOfType<UnsupportedType>();
    }

    [Fact]
    public async Task Decrypt_WithANewerTypeBaselineAndTheWrongAad_Throws()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("future-value")
            .WithAad("row-42")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(WithANewerTypeBaseline(encrypted))
            .WithAad("row-999")
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Fact]
    public async Task Encrypt_SameValueTwice_ProducesDifferentBytes()
    {
        var token = TestContext.Current.CancellationToken;

        var first = await _propertyEncryption.EncryptRequest()
            .FromValue("same-value")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        var second = await _propertyEncryption.EncryptRequest()
            .FromValue("same-value")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        first.Should().NotEqual(second);
    }

    [Fact]
    public async Task Encrypt_WithAnUnsupportedValueType_Throws()
    {
        var token = TestContext.Current.CancellationToken;

        var act = () => _propertyEncryption.EncryptRequest()
            .FromValue(new Dictionary<string, object>())
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Fact]
    public async Task Encrypt_WithAnUnknownKeyAlias_Throws()
    {
        var token = TestContext.Current.CancellationToken;

        var act = () => _propertyEncryption.EncryptRequest()
            .FromValue("value")
            .UsingKeyAlias("no-such-alias")
            .EncryptToBytesAsync(token);

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
    }

    [Fact]
    public async Task Decrypt_WithADriverMissingTheValuesProfile_ThrowsProfileNotFound()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = await _propertyEncryption.EncryptRequest()
            .FromValue("value")
            .UsingKeyAlias("main")
            .EncryptToBytesAsync(token);

        await using var otherDriver = GraphDatabase.Driver(
            "bolt://localhost",
            builder => builder.WithPropertyEncryptionProfiles([EnvelopeProfile("other-profile")]));

        var act = () => otherDriver.PropertyEncryption().DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<EncryptionProfileNotFoundException>().WithMessage("*'test-profile'*");
    }

    [Fact]
    public async Task CreateKey_WithACancelledToken_ThrowsOperationCanceled()
    {
        var cancelled = new CancellationToken(canceled: true);

        var act = () => _propertyEncryption.KeyManager().CreateAsync("other", cancellationToken: cancelled);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EncryptThenDecrypt_AcrossTwoNamedProfiles_UsesTheProfileNamedInTheRequest()
    {
        var token = TestContext.Current.CancellationToken;
        await using var driver = GraphDatabase.Driver(
            "bolt://localhost",
            builder => builder.WithPropertyEncryptionProfiles(
                [EnvelopeProfile("profile-a"), EnvelopeProfile("profile-b")]));

        var propertyEncryption = driver.PropertyEncryption();
        await propertyEncryption.KeyManager("profile-b")
            .CreateAsync("b-key", cancellationToken: TestContext.Current.CancellationToken);

        var encrypted = await propertyEncryption.EncryptRequest()
            .FromValue("profile-b-value")
            .UsingProfile("profile-b")
            .UsingKeyAlias("b-key")
            .EncryptToBytesAsync(token);

        var decrypted = await propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithPersistedAad()
            .DecryptAsync(token);

        decrypted.Should().Be("profile-b-value");
    }

    [Fact]
    public void ConfiguringTwoProfilesWithTheSameName_IsRejectedWhenTheProfilesAreSupplied()
    {
        var act = () => GraphDatabase.Driver(
            "bolt://localhost",
            builder => builder.WithPropertyEncryptionProfiles(
                [EnvelopeProfile("same-name"), EnvelopeProfile("same-name")]));

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Duplicate encryption profile name 'same-name'.*");
    }
}
