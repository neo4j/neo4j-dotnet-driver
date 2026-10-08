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
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class AliasToKeyIdCacheTests
{
    private readonly AliasToKeyIdCache _subject;

    public AliasToKeyIdCacheTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.Now()).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _subject = new AliasToKeyIdCache(clock.Object);
    }

    private static IEnvelopeEncryptionProfile Profile(
        string name = "profile",
        CacheConfig? keyCache = null,
        CacheConfig? aliasIndex = null)
    {
        var profile = new Mock<IEnvelopeEncryptionProfile>();
        profile.SetupGet(p => p.Name).Returns(name);
        profile.SetupGet(p => p.KeyCacheConfig).Returns(keyCache ?? Enabled);
        profile.SetupGet(p => p.KeyAliasIndexConfig).Returns(aliasIndex ?? Enabled);
        return profile.Object;
    }

    private static readonly CacheConfig Enabled = new(100, TimeSpan.FromMinutes(15));


    [Fact]
    public void TryGet_AfterSet_ReturnsCachedKeyId()
    {
        _subject.Set(Profile("profile"), "main", "key-1");

        var found = _subject.TryGet(Profile("profile"), "main", out var keyId);

        found.Should().BeTrue();
        keyId.Should().Be("key-1");
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var found = _subject.TryGet(Profile("profile"), "absent", out var keyId);

        found.Should().BeFalse();
        keyId.Should().BeNull();
    }

    [Fact]
    public void TryGet_SameAliasDifferentProfiles_AreIsolated()
    {
        _subject.Set(Profile("profile-a"), "main", "key-a");
        _subject.Set(Profile("profile-b"), "main", "key-b");

        _subject.TryGet(Profile("profile-a"), "main", out var a);
        _subject.TryGet(Profile("profile-b"), "main", out var b);

        a.Should().Be("key-a");
        b.Should().Be("key-b");
    }
    [Fact]
    public void Set_WhenTheAliasIndexIsDisabled_StoresNothing()
    {
        var disabled = new Mock<IEnvelopeEncryptionProfile>();
        disabled.SetupGet(p => p.Name).Returns("profile");
        disabled.SetupGet(p => p.KeyAliasIndexConfig).Returns((CacheConfig?)null);

        _subject.Set(disabled.Object, "main", "key-1");

        var found = _subject.TryGet(disabled.Object, "main", out var keyId);

        found.Should().BeFalse();
        keyId.Should().BeNull();
    }

    [Fact]
    public void Remove_AfterSet_MakesTheNextTryGetMiss()
    {
        _subject.Set(Profile(), "main", "key-1");

        _subject.Remove(Profile(), "main");
        var found = _subject.TryGet(Profile(), "main", out var keyId);

        found.Should().BeFalse();
        keyId.Should().BeNull();
    }

    [Fact]
    public void Remove_LeavesOtherAliasesInPlace()
    {
        _subject.Set(Profile(), "main", "key-1");
        _subject.Set(Profile(), "spare", "key-2");

        _subject.Remove(Profile(), "main");
        var found = _subject.TryGet(Profile(), "spare", out var keyId);

        found.Should().BeTrue();
        keyId.Should().Be("key-2");
    }

    [Fact]
    public void TryGet_AfterTheConfiguredTtlHasPassed_Misses()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.Now()).Returns(() => now);
        var subject = new AliasToKeyIdCache(clock.Object);
        var profile = Profile(aliasIndex: new CacheConfig(10, TimeSpan.FromSeconds(15)));

        subject.Set(profile, "main", "key-1");
        now = now.AddSeconds(16);

        var found = subject.TryGet(profile, "main", out var keyId);

        found.Should().BeFalse();
        keyId.Should().BeNull();
    }
}
