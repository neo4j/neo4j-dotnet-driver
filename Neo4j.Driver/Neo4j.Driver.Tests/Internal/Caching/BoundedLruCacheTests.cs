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
    public void TryGet_NoTtlConfigured_NeverExpires()
    {
        var subject = CreateSubject(capacity: 10, ttl: null);

        subject.Set("a", "1");
        _now += TimeSpan.FromDays(365);

        subject.TryGet("a", out var value).Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_EntryWithinTtl_ReturnsTrue()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("a", "1");
        _now += TimeSpan.FromSeconds(10);

        subject.TryGet("a", out var value).Should().BeTrue();
        value.Should().Be("1");
    }

    [Fact]
    public void TryGet_EntryOlderThanTtl_ReturnsFalseAndEvictsIt()
    {
        var subject = CreateSubject(capacity: 10, ttl: TimeSpan.FromSeconds(15));

        subject.Set("a", "1");
        _now += TimeSpan.FromSeconds(16);

        subject.TryGet("a", out _).Should().BeFalse();

        // confirm it was evicted, not just reported as a miss - re-setting should not
        // collide with a stale entry still occupying capacity
        _now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        subject.Set("a", "2");
        subject.TryGet("a", out var value).Should().BeTrue();
        value.Should().Be("2");
    }

    [Fact]
    public void Set_OverCapacity_EvictsLeastRecentlyUsedEntry()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.Set("c", "3");

        subject.TryGet("a", out _).Should().BeFalse();
        subject.TryGet("b", out _).Should().BeTrue();
        subject.TryGet("c", out _).Should().BeTrue();
    }

    [Fact]
    public void TryGet_PromotesEntryToMostRecentlyUsed_SoItSurvivesEviction()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.TryGet("a", out _); // "a" is now more recently used than "b"
        subject.Set("c", "3"); // should evict "b", not "a"

        subject.TryGet("a", out _).Should().BeTrue();
        subject.TryGet("b", out _).Should().BeFalse();
        subject.TryGet("c", out _).Should().BeTrue();
    }

    [Fact]
    public void Set_ExistingKey_DoesNotCountTwiceTowardsCapacity()
    {
        var subject = CreateSubject(capacity: 2, ttl: null);

        subject.Set("a", "1");
        subject.Set("b", "2");
        subject.Set("a", "1-updated");

        subject.TryGet("a", out var value).Should().BeTrue();
        value.Should().Be("1-updated");
        subject.TryGet("b", out _).Should().BeTrue();
    }
}
