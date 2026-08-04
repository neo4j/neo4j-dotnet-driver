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
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;
using static Neo4j.Driver.Tests.Internal.Encryption.EncryptionTestHelpers;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EnvelopeDataKeyProviderTests : UnitTestBase
{
    private const string ProfileName = "profile-a";

    private readonly Mock<IKeyEncapsulationService> _kes = new();
    private readonly Mock<IEncapsulatedKeyRepository> _repository = new();

    private static readonly byte[] Encapsulation = [0xBB];
    private static readonly byte[] Dek = Sequence(32, seed: 0x30);
    private static readonly byte[] DataKey = Sequence(32, seed: 0x40);

    private IEnvelopeEncryptionProfile Profile()
    {
        var profile = new Mock<IEnvelopeEncryptionProfile>();
        profile.SetupGet(p => p.Name).Returns(ProfileName);
        profile.SetupGet(p => p.KeyEncapsulationService).Returns(_kes.Object);
        profile.SetupGet(p => p.KeyRepository).Returns(_repository.Object);
        return profile.Object;
    }

    private static EncapsulatedKey Key()
    {
        return new EncapsulatedKey(
            "key-1",
            "main",
            Encapsulation,
            new Dictionary<string, string> { ["iv"] = "wrap-iv" });
    }

    private void StubDecapsulateAndDerive()
    {
        _kes.Setup(k => k.DecapsulateAsync(
                Matches(Encapsulation),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dek);

        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(Dek), 32)).Returns(DataKey);
    }

    [Fact]
    public async Task GetDataKey_ByAliasWithColdCaches_FindsDecapsulatesAndDerives()
    {
        _repository.Setup(r => r.FindAsync(
                new KeyReference("main", KeyReferenceType.Alias),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulateAndDerive();

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(DataKey);
    }

    [Fact]
    public async Task GetDataKey_ByAliasWithColdCaches_PrimesBothCaches()
    {
        _repository.Setup(r => r.FindAsync(
                new KeyReference("main", KeyReferenceType.Alias),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulateAndDerive();

        var aliasCache = Freeze<IAliasToKeyIdCache>();
        var keyCache = Freeze<IEncryptionKeyCache>();

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        aliasCache.Verify(c => c.Set(ProfileName, "main", "key-1"));
        keyCache.Verify(c => c.Set(ProfileName, "key-1", Matches(Dek)));
    }

    [Fact]
    public async Task GetDataKey_AliasCacheHit_SkipsAliasRepositoryLookup()
    {
        string? cachedKeyId = "key-1";
        Freeze<IAliasToKeyIdCache>()
            .Setup(c => c.TryGet(ProfileName, "main", out cachedKeyId))
            .Returns(true);

        _repository.Setup(r => r.FindAsync(
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulateAndDerive();

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(DataKey);
    }

    [Fact]
    public async Task GetDataKey_AliasAndDekCacheHit_NeverTouchesRepositoryOrKes()
    {
        string? cachedKeyId = "key-1";
        Freeze<IAliasToKeyIdCache>()
            .Setup(c => c.TryGet(ProfileName, "main", out cachedKeyId))
            .Returns(true);

        byte[]? cachedDek = Dek;
        Freeze<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(ProfileName, "key-1", out cachedDek))
            .Returns(true);


        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(Dek), 32)).Returns(DataKey);

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("main", KeyReferenceType.Alias),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(DataKey);
    }

    [Fact]
    public async Task GetDataKey_ByKeyId_IgnoresAliasCacheEvenIfPoisoned()
    {
        string? poisonedKeyId = "wrong-id";
        Freeze<IAliasToKeyIdCache>()
            .Setup(c => c.TryGet(ProfileName, It.IsAny<string>(), out poisonedKeyId))
            .Returns(true);

        _repository.Setup(r => r.FindAsync(
                new KeyReference("key-1", KeyReferenceType.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Key());

        StubDecapsulateAndDerive();

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(DataKey);
    }

    [Fact]
    public async Task GetDataKey_ByKeyIdWithDekCacheHit_NeverTouchesRepositoryOrKes()
    {
        byte[]? cachedDek = Dek;
        Freeze<IEncryptionKeyCache>()
            .Setup(c => c.TryGet(ProfileName, "key-1", out cachedDek))
            .Returns(true);

        Freeze<IKeyDerivation>().Setup(d => d.Derive(Matches(Dek), 32)).Returns(DataKey);

        var subject = CreateSubject<EnvelopeDataKeyProvider>();
        var result = await subject.GetDataKeyAsync(
            Profile(),
            new KeyReference("key-1", KeyReferenceType.Id),
            TestContext.Current.CancellationToken);

        result.KeyId.Should().Be("key-1");
        result.DataKey.Should().BeSameAs(DataKey);
    }
}
