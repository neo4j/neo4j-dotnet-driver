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

public class EncryptionKeyCacheTests
{
    private readonly EncryptionKeyCache _subject;

    public EncryptionKeyCacheTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.Now()).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _subject = new EncryptionKeyCache(clock.Object);
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
    public void TryGet_AfterSet_ReturnsCachedKey()
    {
        _subject.Set(Profile("profile"), "key-1", [1, 2, 3]);

        var found = _subject.TryGet(Profile("profile"), "key-1", out var key);

        found.Should().BeTrue();
        key.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var found = _subject.TryGet(Profile("profile"), "absent", out var key);

        found.Should().BeFalse();
        key.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        _subject.Set(Profile("profile"), "key-1", [1, 2, 3]);
        _subject.Set(Profile("profile"), "key-1", [9, 9]);

        _subject.TryGet(Profile("profile"), "key-1", out var key);

        key.Should().Equal(9, 9);
    }

    [Fact]
    public void TryGet_SameKeyIdDifferentProfiles_AreIsolated()
    {
        _subject.Set(Profile("profile-a"), "key-1", [1]);
        _subject.Set(Profile("profile-b"), "key-1", [2]);

        _subject.TryGet(Profile("profile-a"), "key-1", out var a);
        _subject.TryGet(Profile("profile-b"), "key-1", out var b);

        a.Should().Equal(1);
        b.Should().Equal(2);
    }
    [Fact]
    public void Set_WhenTheKeyCacheIsDisabled_StoresNothing()
    {
        var disabled = new Mock<IEnvelopeEncryptionProfile>();
        disabled.SetupGet(p => p.Name).Returns("profile");
        disabled.SetupGet(p => p.KeyCacheConfig).Returns((CacheConfig?)null);

        _subject.Set(disabled.Object, "key-1", [1, 2, 3]);

        var found = _subject.TryGet(disabled.Object, "key-1", out var key);

        found.Should().BeFalse();
        key.Should().BeNull();
    }

    [Fact]
    public void Set_BeyondTheConfiguredMaxSize_EvictsTheLeastRecentlyUsed()
    {
        var profile = Profile(keyCache: new CacheConfig(2, TimeSpan.FromMinutes(15)));

        _subject.Set(profile, "key-1", [1]);
        _subject.Set(profile, "key-2", [2]);
        _subject.TryGet(profile, "key-1", out _);
        _subject.Set(profile, "key-3", [3]);

        _subject.TryGet(profile, "key-2", out var evicted).Should().BeFalse();
        _subject.TryGet(profile, "key-1", out var kept).Should().BeTrue();
        evicted.Should().BeNull();
        kept.Should().Equal(1);
    }

    [Fact]
    public void TryGet_AfterTheConfiguredTtlHasPassed_Misses()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.Now()).Returns(() => now);
        var subject = new EncryptionKeyCache(clock.Object);
        var profile = Profile(keyCache: new CacheConfig(10, TimeSpan.FromSeconds(30)));

        subject.Set(profile, "key-1", [1]);
        now = now.AddSeconds(31);

        var found = subject.TryGet(profile, "key-1", out var key);

        found.Should().BeFalse();
        key.Should().BeNull();
    }
}
