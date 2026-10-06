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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeDataKeyProviderTests
{
    private const string ProfileName = "profile-a";

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<EnvelopeDataKeyProvider>();

    private readonly Mock<IKeyEncapsulationService> _kes = new();
    private readonly Mock<IEncapsulatedKeyRecordRepository> _repository = new();

    private static readonly byte[] Encapsulation = [0xBB];
    private static readonly byte[] Dek = Sequence(32, seed: 0x30);

    private static readonly byte[] ReassignedEncapsulation = [0xCC];
    private static readonly byte[] ReassignedDek = Sequence(32, seed: 0x60);

    private IEnvelopeEncryptionProfile Profile()
    {
        var profile = new Mock<IEnvelopeEncryptionProfile>();
        profile.SetupGet(p => p.Name).Returns(ProfileName);
        profile.SetupGet(p => p.KeyEncapsulationService).Returns(_kes.Object);
        profile.SetupGet(p => p.KeyRepository).Returns(_repository.Object);
        return profile.Object;
    }

    private static EncapsulatedKeyRecord Key(
        string id = "key-1",
        string? alias = "main",
        byte[]? encapsulation = null)
    {
        return new EncapsulatedKeyRecord(
            id,
            alias,
            encapsulation ?? Encapsulation,
            new Dictionary<string, string> { ["iv"] = "wrap-iv" });
    }

    private Mock<IAliasToKeyIdCache> StubAliasIndexHit(string alias, string keyId)
    {
        var aliasIndex = _autoMocker.GetMock<IAliasToKeyIdCache>();
        string? indexed = keyId;
        aliasIndex.Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), alias, out indexed))
            .Returns(true);

        return aliasIndex;
    }

    private void StubDecapsulate()
    {
        _kes.Setup(k => k.DecapsulateAsync(
                Matches(Encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dek);

        _kes.Setup(k => k.DecapsulateAsync(
                Matches(ReassignedEncapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReassignedDek);
    }

    [Fact]
    public async Task GetDataKey_ByAliasWithColdCaches_FindsAndDecapsulates()
    {
        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulate();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(Dek);
    }

    [Fact]
    public async Task GetDataKey_WhenTheAliasIsNotInTheRepository_ThrowsAliasNotFound()
    {
        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncapsulatedKeyRecord?)null);

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var act = async () => await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>().WithMessage("*main*");
    }

    [Fact]
    public async Task GetDataKey_WhenTheIdIsNotInTheRepository_ThrowsKeyNotFound()
    {
        _repository.Setup(r => r.FindByIdAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncapsulatedKeyRecord?)null);

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var act = async () => await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedKeyNotFoundException>().WithMessage("*key-1*");
    }

    private void StubDecapsulateToAnAes128Key()
    {
        _repository.Setup(r => r.FindByIdAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        _kes.Setup(k => k.DecapsulateAsync(
                Matches(Encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sequence(16, seed: 0x30));
    }

    [Fact]
    public async Task GetDataKey_WhenTheDecapsulatedKeyIsNotAes256_Throws()
    {
        StubDecapsulateToAnAes128Key();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var act = async () => await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<PropertyEncryptionException>().WithMessage("*key-1*16*");
    }

    [Fact]
    public async Task GetDataKey_WhenTheDecapsulatedKeyIsNotAes256_DoesNotCacheIt()
    {
        StubDecapsulateToAnAes128Key();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var act = async () => await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<PropertyEncryptionException>();
        _autoMocker.GetMock<IEncryptionKeyCache>().Verify(
            c => c.Set(It.IsAny<IEnvelopeEncryptionProfile>(), It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDataKey_ByAliasWithColdCaches_PrimesBothCaches()
    {
        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulate();

        var aliasCache = _autoMocker.GetMock<IAliasToKeyIdCache>();
        var keyCache = _autoMocker.GetMock<IEncryptionKeyCache>();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        aliasCache.Verify(c => c.Set(It.IsAny<IEnvelopeEncryptionProfile>(), "main", "key-1"));
        keyCache.Verify(c => c.Set(It.IsAny<IEnvelopeEncryptionProfile>(), "key-1", Matches(Dek)));
    }

    [Fact]
    public async Task GetDataKey_ByAliasWhoseIndexedKeyWasDeleted_ResolvesTheAliasToItsCurrentKey()
    {
        StubAliasIndexHit("main", "key-1");

        _repository.Setup(r => r.FindByIdAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncapsulatedKeyRecord?)null);

        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key("key-2", "main", ReassignedEncapsulation));

        StubDecapsulate();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-2");
        result.DataKey.Should().BeSameAs(ReassignedDek);
    }

    [Fact]
    public async Task GetDataKey_ByAliasReassignedWhileItsOldKeySurvives_ResolvesTheAliasToItsCurrentKey()
    {
        StubAliasIndexHit("main", "key-1");

        _repository.Setup(r => r.FindByIdAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key("key-1", alias: null));

        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key("key-2", "main", ReassignedEncapsulation));

        StubDecapsulate();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-2");
        result.DataKey.Should().BeSameAs(ReassignedDek);
    }

    [Fact]
    public async Task GetDataKey_ByAliasWithAColdIndexButACachedKey_DoesNotDecapsulateAgain()
    {
        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        byte[]? cachedDek = Dek;
        _autoMocker.GetMock<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), "key-1", out cachedDek))
            .Returns(true);

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.DataKey.Should().BeSameAs(Dek);
        _kes.Verify(
            k => k.DecapsulateAsync(
                It.IsAny<byte[]>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDataKey_ByAnIndexedAliasThatNoLongerExists_DropsTheStaleMapping()
    {
        var aliasIndex = StubAliasIndexHit("main", "key-1");

        _repository.Setup(r => r.FindByAliasAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncapsulatedKeyRecord?)null);

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var act = async () => await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EncapsulatedAliasNotFoundException>();
        aliasIndex.Verify(c => c.Remove(It.IsAny<IEnvelopeEncryptionProfile>(), "main"));
    }

    [Fact]
    public async Task GetDataKey_AliasAndDekCacheHit_NeverTouchesRepositoryOrKes()
    {
        string? cachedKeyId = "key-1";
        _autoMocker.GetMock<IAliasToKeyIdCache>()
            .Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), "main", out cachedKeyId))
            .Returns(true);

        byte[]? cachedDek = Dek;
        _autoMocker.GetMock<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), "key-1", out cachedDek))
            .Returns(true);


        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(Dek);
    }

    [Fact]
    public async Task GetDataKey_ByKeyId_IgnoresAliasCacheEvenIfPoisoned()
    {
        string? poisonedKeyId = "wrong-id";
        _autoMocker.GetMock<IAliasToKeyIdCache>()
            .Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), It.IsAny<string>(), out poisonedKeyId))
            .Returns(true);

        _repository.Setup(r => r.FindByIdAsync("key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulate();

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(Dek);
    }

    [Fact]
    public async Task GetDataKey_ByKeyIdWithDekCacheHit_NeverTouchesRepositoryOrKes()
    {
        byte[]? cachedDek = Dek;
        _autoMocker.GetMock<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(It.IsAny<IEnvelopeEncryptionProfile>(), "key-1", out cachedDek))
            .Returns(true);

        var subject = _autoMocker.CreateInstance<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(Dek);
    }
}
