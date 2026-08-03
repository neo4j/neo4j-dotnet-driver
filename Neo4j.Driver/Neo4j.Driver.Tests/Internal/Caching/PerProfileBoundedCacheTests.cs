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
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Caching;

public class PerProfileBoundedCacheTests
{
    private DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _clock = new();

    public PerProfileBoundedCacheTests()
    {
        _clock.Setup(c => c.Now()).Returns(() => _now);
    }

    private PerProfileBoundedCache<string> CreateSubject(int capacityPerProfile, TimeSpan? ttl)
    {
        return new PerProfileBoundedCache<string>(capacityPerProfile, ttl, _clock.Object);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedValue()
    {
        var subject = CreateSubject(capacityPerProfile: 10, ttl: null);

        subject.Set("profile-a", "k1", "v1");
        var found = subject.TryGet("profile-a", "k1", out var value);

        found.Should().BeTrue();
        value.Should().Be("v1");
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var subject = CreateSubject(capacityPerProfile: 10, ttl: null);

        var found = subject.TryGet("profile-a", "absent", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_SameKeyDifferentProfiles_AreIsolated()
    {
        var subject = CreateSubject(capacityPerProfile: 10, ttl: null);

        subject.Set("profile-a", "k1", "va");
        subject.Set("profile-b", "k1", "vb");

        subject.TryGet("profile-a", "k1", out var a);
        subject.TryGet("profile-b", "k1", out var b);

        a.Should().Be("va");
        b.Should().Be("vb");
    }

    [Fact]
    public void Set_OverCapacityInOneProfile_DoesNotEvictAnotherProfilesEntries()
    {
        var subject = CreateSubject(capacityPerProfile: 1, ttl: null);

        subject.Set("profile-a", "k1", "va1");
        subject.Set("profile-b", "k1", "vb1");
        subject.Set("profile-a", "k2", "va2"); // should evict profile-a's k1 only

        subject.TryGet("profile-a", "k1", out _).Should().BeFalse();
        subject.TryGet("profile-a", "k2", out var va2).Should().BeTrue();
        va2.Should().Be("va2");
        subject.TryGet("profile-b", "k1", out var vb1).Should().BeTrue();
        vb1.Should().Be("vb1");
    }

    [Fact]
    public void TryGet_EntryOlderThanTtl_ReturnsFalse()
    {
        var subject = CreateSubject(capacityPerProfile: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("profile-a", "k1", "v1");
        _now += TimeSpan.FromSeconds(16);

        subject.TryGet("profile-a", "k1", out _).Should().BeFalse();
    }
}
