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
using Neo4j.Driver.Internal.Caching;
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.Preview.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Caching;

public class PerProfileBoundedCacheTests
{
    private static readonly CacheConfig Roomy = new(10, TimeSpan.FromDays(365));

    private DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _clock = new();

    public PerProfileBoundedCacheTests()
    {
        _clock.Setup(c => c.Now()).Returns(() => _now);
    }

    private PerProfileBoundedCache<string> CreateSubject()
    {
        return new PerProfileBoundedCache<string>(_clock.Object);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedValue()
    {
        var subject = CreateSubject();

        subject.Set("profile-a", Roomy, "k1", "v1");
        var found = subject.TryGet("profile-a", Roomy, "k1", out var value);

        found.Should().BeTrue();
        value.Should().Be("v1");
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var subject = CreateSubject();

        var found = subject.TryGet("profile-a", Roomy, "absent", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_SameKeyDifferentProfiles_AreIsolated()
    {
        var subject = CreateSubject();

        subject.Set("profile-a", Roomy, "k1", "va");
        subject.Set("profile-b", Roomy, "k1", "vb");
        subject.TryGet("profile-a", Roomy, "k1", out var a);
        subject.TryGet("profile-b", Roomy, "k1", out var b);

        a.Should().Be("va");
        b.Should().Be("vb");
    }

    [Fact]
    public void Set_OverCapacityInOneProfile_DoesNotEvictAnotherProfilesEntries()
    {
        var singleEntry = new CacheConfig(1, TimeSpan.FromDays(365));
        var subject = CreateSubject();

        subject.Set("profile-a", singleEntry, "k1", "va1");
        subject.Set("profile-b", singleEntry, "k1", "vb1");
        subject.Set("profile-a", singleEntry, "k2", "va2");
        var foundA1 = subject.TryGet("profile-a", singleEntry, "k1", out _);
        var foundA2 = subject.TryGet("profile-a", singleEntry, "k2", out var va2);
        var foundB1 = subject.TryGet("profile-b", singleEntry, "k1", out var vb1);

        foundA1.Should().BeFalse();
        foundA2.Should().BeTrue();
        va2.Should().Be("va2");
        foundB1.Should().BeTrue();
        vb1.Should().Be("vb1");
    }

    [Fact]
    public void TryGet_EntryOlderThanTtl_ReturnsFalse()
    {
        var fifteenSeconds = new CacheConfig(10, TimeSpan.FromSeconds(15));
        var subject = CreateSubject();

        subject.Set("profile-a", fifteenSeconds, "k1", "v1");
        _now += TimeSpan.FromSeconds(16);
        var found = subject.TryGet("profile-a", fifteenSeconds, "k1", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void Remove_DropsTheKeyFromThatProfileOnly()
    {
        var subject = CreateSubject();

        subject.Set("profile-a", Roomy, "k1", "va");
        subject.Set("profile-b", Roomy, "k1", "vb");
        subject.Remove("profile-a", "k1");
        var foundA = subject.TryGet("profile-a", Roomy, "k1", out _);
        var foundB = subject.TryGet("profile-b", Roomy, "k1", out var b);

        foundA.Should().BeFalse();
        foundB.Should().BeTrue();
        b.Should().Be("vb");
    }
}
