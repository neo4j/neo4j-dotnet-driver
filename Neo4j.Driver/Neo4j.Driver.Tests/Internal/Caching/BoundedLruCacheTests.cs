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

public class BoundedLruCacheTests
{
    private DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _clock = new();

    public BoundedLruCacheTests()
    {
        _clock.Setup(c => c.Now()).Returns(() => _now);
    }

    private BoundedLruCache<string, string> CreateSubject(int capacity, TimeSpan? ttl)
    {
        return new BoundedLruCache<string, string>(capacity, ttl, _clock.Object);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedValue()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        subject.Set("a", "1");
        var found = subject.TryGet("a", out var value);

        found.Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        var found = subject.TryGet("absent", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        subject.Set("a", "1");
        subject.Set("a", "2");
        subject.TryGet("a", out var value);

        value.Should().Be("2");
    }

    [Fact]
    public void TryGet_TtlBeyondTheLatestRepresentableTime_ReturnsCachedValue()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.MaxValue);

        subject.Set("a", "1");
        var found = subject.TryGet("a", out var value);

        found.Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_NoTtlConfigured_NeverExpires()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        subject.Set("a", "1");
        _now += TimeSpan.FromDays(365);
        var found = subject.TryGet("a", out var value);

        found.Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_EntryWithinTtl_ReturnsTrue()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("a", "1");
        _now += TimeSpan.FromSeconds(10);
        var found = subject.TryGet("a", out var value);

        found.Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_EntryOlderThanTtl_ReturnsFalseAndEvictsIt()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("a", "1");
        _now += TimeSpan.FromSeconds(16);
        var foundWhenExpired = subject.TryGet("a", out _);

        _now -= TimeSpan.FromSeconds(16);
        var foundAfterRewinding = subject.TryGet("a", out _);

        foundWhenExpired.Should().BeFalse();
        foundAfterRewinding.Should().BeFalse();
    }

    [Fact]
    public void Set_OverCapacity_EvictsLeastRecentlyUsedEntry()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.Set("c", "3");
        var foundA = subject.TryGet("a", out _);
        var foundB = subject.TryGet("b", out _);
        var foundC = subject.TryGet("c", out _);

        foundA.Should().BeFalse();
        foundB.Should().BeTrue();
        foundC.Should().BeTrue();
    }

    [Fact]
    public void TryGet_PromotesEntryToMostRecentlyUsed_SoItSurvivesEviction()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.TryGet("a", out _);
        subject.Set("c", "3");
        var foundA = subject.TryGet("a", out _);
        var foundB = subject.TryGet("b", out _);
        var foundC = subject.TryGet("c", out _);

        foundA.Should().BeTrue();
        foundB.Should().BeFalse();
        foundC.Should().BeTrue();
    }

    [Fact]
    public void Set_WhenAnExpiredEntryIsNotTheLeastRecentlyUsed_PurgesItRatherThanEvictingALiveEntry()
    {
        const string expiredButMostRecentlyUsed = "set-at-0s";
        const string liveButLeastRecentlyUsed = "set-at-50s";
        const string incoming = "set-at-110s";

        // 2 slots, cache entries expire after 100s
        var subject = CreateSubject(capacity: 2, ttl: TimeSpan.FromSeconds(100));

        // T+0s: takes the first slot. Expires at T+100s.
        subject.Set(expiredButMostRecentlyUsed, "1");

        // T+50s: add another, so cache is now full. Expires at T+150s.
        _now += TimeSpan.FromSeconds(50);
        subject.Set(liveButLeastRecentlyUsed, "2");

        // T+90s: inside the TTL, so read succeeds and makes it the most-recently-used
        // it still expires at T+100s
        _now += TimeSpan.FromSeconds(40);
        subject.TryGet(expiredButMostRecentlyUsed, out _).Should().BeTrue();

        // T+110s: recency order and expiry order now disagree
        //   most recently used  -> expiredButMostRecentlyUsed, dead since T+100s
        //   least recently used -> liveButLeastRecentlyUsed, alive until T+150s
        _now += TimeSpan.FromSeconds(20);

        // Adding another means one must be purged. The expired entry should be purged, even though it is
        // the most recently used
        subject.Set(incoming, "3");

        // the expired entry should be gone
        var expiredEntrySurvived = subject.TryGet(expiredButMostRecentlyUsed, out _);
        expiredEntrySurvived.Should().BeFalse();

        // the live entry should still be there
        var liveEntrySurvived = subject.TryGet(liveButLeastRecentlyUsed, out var value);
        liveEntrySurvived.Should().BeTrue();
        value.Should().Be("2");

        // the new entry should be there
        var newEntrySurvived = subject.TryGet(incoming, out value);
        newEntrySurvived.Should().BeTrue();
        value.Should().Be("3");
    }

    [Fact]
    public void Set_ExistingKey_DoesNotCountTwiceTowardsCapacity()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.Set("a", "1-updated");
        var foundA = subject.TryGet("a", out var value);
        var foundB = subject.TryGet("b", out _);

        foundA.Should().BeTrue();
        value.Should().Be("1-updated");
        foundB.Should().BeTrue();
    }

    [Fact]
    public void TryGet_EntryExactlyAtItsTtl_ReturnsFalse()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("a", "1");
        _now += TimeSpan.FromSeconds(15);
        var found = subject.TryGet("a", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void Remove_DropsOnlyThatKey()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.Remove("a");
        var foundA = subject.TryGet("a", out _);
        var foundB = subject.TryGet("b", out _);

        foundA.Should().BeFalse();
        foundB.Should().BeTrue();
    }
}
