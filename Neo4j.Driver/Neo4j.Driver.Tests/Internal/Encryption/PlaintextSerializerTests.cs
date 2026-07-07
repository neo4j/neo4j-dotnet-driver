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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Encryption;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Encryption;

public class PlaintextSerializerTests
{
    private readonly PlaintextSerializer _subject = new();

    public static IEnumerable<object[]> ScalarPropertyValues => new[]
    {
        new object[] { true },
        new object[] { false },
        new object[] { 0L },
        new object[] { 42L },
        new object[] { -1L },
        new object[] { long.MaxValue },
        new object[] { long.MinValue },
        new object[] { 3.14 },
        new object[] { 0.0 },
        new object[] { "hello" },
        new object[] { "" },
        new object[] { "unicode ☃ 日本語" }
    };

    [Theory]
    [MemberData(nameof(ScalarPropertyValues))]
    public void RoundTripsScalarPropertyValue(object value)
    {
        var result = _subject.Deserialize(_subject.Serialize(value));

        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void RoundTripsByteArray()
    {
        var value = new byte[] { 0x00, 0x01, 0xFE, 0xFF };

        var result = _subject.Deserialize(_subject.Serialize(value));

        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void RoundTripsHomogeneousList()
    {
        var value = new List<long> { 1L, 2L, 3L };

        var result = _subject.Deserialize(_subject.Serialize(value));

        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void SerializeProducesNonEmptyPlaintext()
    {
        _subject.Serialize("x").Should().NotBeNullOrEmpty();
    }
}
