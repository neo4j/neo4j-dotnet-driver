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
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Preview.Encryption;
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
        var key = await _propertyEncryption.KeyManager().CreateAsync("main");
        _keyId = key.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }

    private static IPropertyEncryptionProfile EnvelopeProfile(string name)
    {
        var kes = new LocalKeyEncapsulationService(
            Kek,
            new AesGcmCipher(),
            new CryptoRandomProvider(),
            new Base64Codec());

        return PropertyEncryptionProfile.Envelope(name, kes, new InMemoryEncapsulatedKeyRepository(new KeyIdGenerator()));
    }

    public static TheoryData<object> SupportedValues() => new()
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
        var codec = new EncryptedValueBytesCodec(
            new EncryptedStructureCodec(
                new MessageFormatFactory(TestDriverContext.MockContext),
                new PackStreamMemorySerializer(new PackStreamReaderWriterFactory())));

        var structure = codec.Decode(encrypted);
        structure.CipherOutput[0] ^= 0xFF;
        return codec.Encode(structure);
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
    public async Task EncryptThenDecrypt_AcrossTwoNamedProfiles_UsesTheProfileNamedInTheRequest()
    {
        var token = TestContext.Current.CancellationToken;
        await using var driver = GraphDatabase.Driver(
            "bolt://localhost",
            builder => builder.WithPropertyEncryptionProfiles(
                [EnvelopeProfile("profile-a"), EnvelopeProfile("profile-b")]));

        var propertyEncryption = driver.PropertyEncryption();
        await propertyEncryption.KeyManager("profile-b").CreateAsync("b-key");

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
