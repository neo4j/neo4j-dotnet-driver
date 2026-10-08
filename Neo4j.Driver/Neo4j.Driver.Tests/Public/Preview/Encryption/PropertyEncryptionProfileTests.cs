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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Encryption;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Public.Preview.Encryption;

public class PropertyEncryptionProfileTests
{
    private static IEnvelopeProfileBuilder Builder()
    {
        return PropertyEncryptionProfile.EnvelopeBuilder(
            "profile-name",
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRecordRepository>());
    }

    private static IEnvelopeEncryptionProfile Envelope(IPropertyEncryptionProfile profile)
    {
        return profile.Should().BeAssignableTo<IEnvelopeEncryptionProfile>().Subject;
    }

    [Fact]
    public void Build_ReturnsAProfileWithTheGivenName()
    {
        var profile = Builder().Build();

        profile.Name.Should().Be("profile-name");
    }

    [Fact]
    public void Build_CarriesTheKeyEncapsulationServiceAndRepository()
    {
        var kes = Mock.Of<IKeyEncapsulationService>();
        var repository = Mock.Of<IEncapsulatedKeyRecordRepository>();

        var profile = PropertyEncryptionProfile.EnvelopeBuilder("profile-name", kes, repository).Build();

        var envelope = Envelope(profile);
        envelope.KeyEncapsulationService.Should().BeSameAs(kes);
        envelope.KeyRepository.Should().BeSameAs(repository);
    }

    [Fact]
    public void Build_ReturnsADistinctInstancePerCall()
    {
        var builder = Builder();

        var first = builder.Build();
        var second = builder.Build();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Build_ByDefault_EnablesBothCachesAtTheAdrDefaults()
    {
        var envelope = Envelope(Builder().Build());

        envelope.KeyCacheConfig.Should().Be(new CacheConfig(100, TimeSpan.FromMinutes(15)));
        envelope.KeyAliasIndexConfig.Should().Be(new CacheConfig(100, TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public void WithKeyCache_ReplacesTheKeyCacheBounds()
    {
        var envelope = Envelope(Builder().WithKeyCache(250, TimeSpan.FromHours(1)).Build());

        envelope.KeyCacheConfig.Should().Be(new CacheConfig(250, TimeSpan.FromHours(1)));
        envelope.KeyAliasIndexConfig.Should().Be(new CacheConfig(100, TimeSpan.FromSeconds(15)));
    }

    [Fact]
    public void WithKeyAliasIndex_ReplacesTheAliasIndexBounds()
    {
        var envelope = Envelope(Builder().WithKeyAliasIndex(5, TimeSpan.FromSeconds(30)).Build());

        envelope.KeyAliasIndexConfig.Should().Be(new CacheConfig(5, TimeSpan.FromSeconds(30)));
        envelope.KeyCacheConfig.Should().Be(new CacheConfig(100, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void WithoutKeyAliasIndex_DisablesOnlyTheAliasIndex()
    {
        var envelope = Envelope(Builder().WithoutKeyAliasIndex().Build());

        envelope.KeyAliasIndexConfig.Should().BeNull();
        envelope.KeyCacheConfig.Should().Be(new CacheConfig(100, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void WithoutKeyCache_DisablesTheAliasIndexWithIt()
    {
        var envelope = Envelope(Builder().WithoutKeyCache().Build());

        envelope.KeyCacheConfig.Should().BeNull();
        envelope.KeyAliasIndexConfig.Should().BeNull();
    }

    [Fact]
    public void Build_WithTheAliasIndexReEnabledAfterDisablingTheKeyCache_Throws()
    {
        var builder = Builder().WithoutKeyCache().WithKeyAliasIndex(10, TimeSpan.FromSeconds(5));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>().WithMessage("*key cache*");
    }

    [Fact]
    public void WithKeyCache_IsOrderIndependentWithWithoutKeyAliasIndex()
    {
        var first = Envelope(Builder().WithoutKeyAliasIndex().WithKeyCache(7, TimeSpan.FromMinutes(2)).Build());
        var second = Envelope(Builder().WithKeyCache(7, TimeSpan.FromMinutes(2)).WithoutKeyAliasIndex().Build());

        first.KeyCacheConfig.Should().Be(second.KeyCacheConfig);
        first.KeyAliasIndexConfig.Should().Be(second.KeyAliasIndexConfig);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithKeyCache_WithANonPositiveMaxSize_Throws(int maxSize)
    {
        var act = () => Builder().WithKeyCache(maxSize, TimeSpan.FromMinutes(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithKeyAliasIndex_WithANonPositiveMaxSize_Throws(int maxSize)
    {
        var act = () => Builder().WithKeyAliasIndex(maxSize, TimeSpan.FromMinutes(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithKeyCache_WithANonPositiveTtl_Throws()
    {
        var act = () => Builder().WithKeyCache(10, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithKeyAliasIndex_WithANonPositiveTtl_Throws()
    {
        var act = () => Builder().WithKeyAliasIndex(10, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnvelopeBuilder_WithNullOrWhitespaceName_Throws(string? name)
    {
        var act = () => PropertyEncryptionProfile.EnvelopeBuilder(
            name!,
            Mock.Of<IKeyEncapsulationService>(),
            Mock.Of<IEncapsulatedKeyRecordRepository>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnvelopeBuilder_WithNullKeyEncapsulationService_Throws()
    {
        var act = () => PropertyEncryptionProfile.EnvelopeBuilder(
            "profile-name",
            null!,
            Mock.Of<IEncapsulatedKeyRecordRepository>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnvelopeBuilder_WithNullKeyRepository_Throws()
    {
        var act = () => PropertyEncryptionProfile.EnvelopeBuilder(
            "profile-name",
            Mock.Of<IKeyEncapsulationService>(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
