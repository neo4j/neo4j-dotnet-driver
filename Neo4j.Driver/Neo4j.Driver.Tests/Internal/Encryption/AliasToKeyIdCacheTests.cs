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

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedKeyId()
    {
        _subject.Set("profile", "main", "key-1");

        var found = _subject.TryGet("profile", "main", out var keyId);

        found.Should().BeTrue();
        keyId.Should().Be("key-1");
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var found = _subject.TryGet("profile", "absent", out var keyId);

        found.Should().BeFalse();
        keyId.Should().BeNull();
    }

    [Fact]
    public void TryGet_SameAliasDifferentProfiles_AreIsolated()
    {
        _subject.Set("profile-a", "main", "key-a");
        _subject.Set("profile-b", "main", "key-b");

        _subject.TryGet("profile-a", "main", out var a);
        _subject.TryGet("profile-b", "main", out var b);

        a.Should().Be("key-a");
        b.Should().Be("key-b");
    }
}
