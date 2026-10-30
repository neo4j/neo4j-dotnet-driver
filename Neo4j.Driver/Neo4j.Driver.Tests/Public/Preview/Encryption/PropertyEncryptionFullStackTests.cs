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
            Guid.Parse("d3b07384-d9a0-4c19-8a3e-1f5c6a7b8c9d"),
            new LocalDate(2026, 10, 6),
            new LocalTime(12, 30, 15, 123456789),
            new LocalDateTime(2026, 10, 6, 12, 30, 15, 123456789),
            new OffsetTime(12, 30, 15, 123456789, 3600),
            new Duration(14, 3, 3723, 500_000_000),
            new Point(7203, 1.5, 2.5),
            new Point(9157, 1.5, 2.5, 3.5),
            new ZonedDateTime(new DateTime(2026, 10, 6, 12, 30, 15), 3600),
            new ZonedDateTime(new DateTime(2026, 10, 6, 12, 30, 15), "Europe/Stockholm"),
            new List<object> { 1L, 2L, 3L },
            new List<object> { "a", "b" },
            new List<object> { Guid.Parse("d3b07384-d9a0-4c19-8a3e-1f5c6a7b8c9d"), Guid.Empty },
            new List<object> { new LocalDate(2026, 10, 6), new LocalDate(2026, 10, 7) },
            new List<object> { new LocalTime(12, 30, 0), new LocalTime(13, 45, 10, 5) },
            new List<object> { new LocalDateTime(2026, 10, 6, 12, 30, 0), new LocalDateTime(2026, 10, 7, 1, 2, 3) },
            new List<object> { new OffsetTime(12, 30, 0, 3600), new OffsetTime(1, 2, 3, -7200) },
            new List<object>
            {
                new ZonedDateTime(new DateTime(2026, 10, 6, 12, 30, 0), 3600),
                new ZonedDateTime(new DateTime(2026, 10, 6, 12, 30, 0), "Europe/Stockholm")
            },
            new List<object> { new Duration(1, 2, 3, 4), new Duration(0, 0, 0, 0) },
            new List<object> { new Point(7203, 1.0, 2.0), new Point(7203, 3.0, 4.0) },
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
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().BeEquivalentTo(value);
    }

    [Fact]
    public async Task EncryptThenDecrypt_ADateOnly_DecryptsAsALocalDate()
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await EncryptByAliasAsync(new DateOnly(2026, 10, 6), token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be(new LocalDate(2026, 10, 6));
    }

    [Fact]
    public async Task EncryptThenDecrypt_ATimeOnly_DecryptsAsALocalTime()
    {
        var token = TestContext.Current.CancellationToken;
        var timeOnly = new TimeOnly(12, 30, 15).Add(TimeSpan.FromTicks(1234567));

        var encrypted = await EncryptByAliasAsync(timeOnly, token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be(new LocalTime(12, 30, 15, 123456700));
    }

    [Fact]
    public async Task EncryptThenDecrypt_AnUnzonedDateTime_DecryptsAsALocalDateTime()
    {
        var token = TestContext.Current.CancellationToken;
        var dateTime = new DateTime(2026, 10, 6, 12, 30, 15, DateTimeKind.Unspecified).AddTicks(1234567);

        var encrypted = await EncryptByAliasAsync(dateTime, token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be(new LocalDateTime(2026, 10, 6, 12, 30, 15, 123456700));
    }

    [Fact]
    public async Task EncryptThenDecrypt_ADateTimeOffset_DecryptsAsAZonedDateTime()
    {
        var token = TestContext.Current.CancellationToken;
        var dateTimeOffset = new DateTimeOffset(2026, 10, 6, 12, 30, 15, TimeSpan.FromHours(1));

        var encrypted = await EncryptByAliasAsync(dateTimeOffset, token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be(new ZonedDateTime(dateTimeOffset));
    }

    [Fact]
    public async Task EncryptThenDecrypt_AUtcDateTime_DecryptsAsAZonedDateTimeAtUtc()
    {
        var token = TestContext.Current.CancellationToken;
        var utc = new DateTime(2026, 10, 6, 12, 30, 15, DateTimeKind.Utc);

        var encrypted = await EncryptByAliasAsync(utc, token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be(new ZonedDateTime(utc, 0));
    }

    public static TheoryData<Vector> Vectors()
    {
        return new()
        {
            Vector.Create(new[] { 1.5f, -2.5f, 3.25f }),
            Vector.Create(new[] { 1.5, -2.5, 3.25 }),
            Vector.Create(new long[] { 1, -2, 3 }),
            Vector.Create(new[] { 1, -2, 3 }),
            Vector.Create(new short[] { 1, -2, 3 }),
            Vector.Create(new sbyte[] { 1, -2, 3 })
        };
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public async Task EncryptThenDecrypt_AVector_RoundTripsTheElementsAndTheirType(Vector vector)
    {
        var token = TestContext.Current.CancellationToken;

        var encrypted = await EncryptByAliasAsync(vector, token);
        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        var roundTripped = decrypted.Should().BeAssignableTo<Vector>().Subject;
        roundTripped.ElementType.Should().Be(vector.ElementType);
        roundTripped.Equals(vector).Should().BeTrue();
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
            .WithoutExternalAad()
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
            .WithoutExternalAad()
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
            .WithoutExternalAad()
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
            .WithoutExternalAad()
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
            .WithoutExternalAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Fact]
    public async Task Decrypt_WithBytesTrailingTheStructure_IgnoresThem()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = await EncryptByAliasAsync("value", token);
        byte[] withTrailingByte = [..encrypted, 0x01];

        var decrypted = await _propertyEncryption.DecryptRequest()
            .FromValue(withTrailingByte)
            .WithoutExternalAad()
            .DecryptAsync(token);

        decrypted.Should().Be("value");
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
            .WithoutExternalAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
    }

    [Fact]
    public async Task Decrypt_WithAnUnknownEncodingVersion_Throws()
    {
        var token = TestContext.Current.CancellationToken;
        var encrypted = await EncryptByAliasAsync("value", token);
        encrypted[0] = 0x02;

        var act = () => _propertyEncryption.DecryptRequest()
            .FromValue(encrypted)
            .WithoutExternalAad()
            .DecryptAsync(token);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
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
            .WithoutExternalAad()
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
    public async Task Encrypt_ByKeyId_AfterTheKeyIsDeleted_Throws()
    {
        var token = TestContext.Current.CancellationToken;
        await _propertyEncryption.EncryptRequest().FromValue("warm").UsingKeyId(_keyId).EncryptToBytesAsync(token);
        await _propertyEncryption.KeyManager().DeleteByIdAsync(_keyId, token);

        var act = () => _propertyEncryption.EncryptRequest()
            .FromValue("value")
            .UsingKeyId(_keyId)
            .EncryptToBytesAsync(token);

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>();
    }

    [Fact]
    public async Task Encrypt_ByKeyAlias_AfterTheKeyIsDeleted_Throws()
    {
        var token = TestContext.Current.CancellationToken;
        await EncryptByAliasAsync("warm", token);
        await _propertyEncryption.KeyManager().DeleteByIdAsync(_keyId, token);

        var act = () => EncryptByAliasAsync("value", token);

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
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
            .WithoutExternalAad()
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
            .WithoutExternalAad()
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
