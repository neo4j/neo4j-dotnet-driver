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

public class BoltValueSerializationSchemeVersionTests
{
    [Fact]
    public void GreaterThan_WhenMajorIsHigher_ReturnsTrueRegardlessOfMinor()
    {
        var higher = new BoltValueSerializationSchemeVersion(7, 0);
        var lower = new BoltValueSerializationSchemeVersion(6, 9);

        (higher > lower).Should().BeTrue();
    }

    [Fact]
    public void GreaterThan_WhenMajorIsLower_ReturnsFalseRegardlessOfMinor()
    {
        var lower = new BoltValueSerializationSchemeVersion(6, 9);
        var higher = new BoltValueSerializationSchemeVersion(7, 0);

        (lower > higher).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_WhenMajorEqualAndMinorIsHigher_ReturnsTrue()
    {
        var higher = new BoltValueSerializationSchemeVersion(6, 2);
        var lower = new BoltValueSerializationSchemeVersion(6, 1);

        (higher > lower).Should().BeTrue();
    }

    [Fact]
    public void GreaterThan_WhenVersionsAreEqual_ReturnsFalse()
    {
        var a = new BoltValueSerializationSchemeVersion(6, 1);
        var b = new BoltValueSerializationSchemeVersion(6, 1);

        (a > b).Should().BeFalse();
    }
}
