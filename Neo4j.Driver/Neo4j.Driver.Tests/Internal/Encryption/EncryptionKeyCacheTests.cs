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

using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class EncryptionKeyCacheTests
{
    private readonly EncryptionKeyCache _subject = new();

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedKey()
    {
        _subject.Set("profile", "key-1", [1, 2, 3]);

        var found = _subject.TryGet("profile", "key-1", out var key);

        found.Should().BeTrue();
        key.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void TryGet_Miss_ReturnsFalse()
    {
        var found = _subject.TryGet("profile", "absent", out var key);

        found.Should().BeFalse();
        key.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        _subject.Set("profile", "key-1", [1, 2, 3]);
        _subject.Set("profile", "key-1", [9, 9]);

        _subject.TryGet("profile", "key-1", out var key);

        key.Should().Equal(9, 9);
    }

    [Fact]
    public void TryGet_SameKeyIdDifferentProfiles_AreIsolated()
    {
        _subject.Set("profile-a", "key-1", [1]);
        _subject.Set("profile-b", "key-1", [2]);

        _subject.TryGet("profile-a", "key-1", out var a);
        _subject.TryGet("profile-b", "key-1", out var b);

        a.Should().Equal(1);
        b.Should().Equal(2);
    }
}
