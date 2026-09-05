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

using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiDurationCodecTests
{
    private readonly QueryApiDurationCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new Duration(0, 0, 0, 0), "P0M0DT0S"],
        [new Duration(1, 2, 3, 4), "P1M2DT3.000000004S"],
        [new Duration(-1, -2, -3, 0), "P-1M-2DT-3S"],
        [new Duration(0, 0, -1, 1), "P0M0DT-0.999999999S"],
        [new Duration(0, 0, -4, 500000000), "P0M0DT-3.500000000S"],
        [new Duration(5, 0, 0, 123456000), "P5M0DT0.123456000S"],
        [new Duration(0, 0, long.MaxValue, 0), "P0M0DT9223372036854775807S"],
        [new Duration(0, 0, long.MinValue, 0), "P0M0DT-9223372036854775808S"]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["PT0S", new Duration(0, 0, 0, 0)],
        ["P1Y", new Duration(12, 0, 0, 0)],
        ["P1W", new Duration(0, 7, 0, 0)],
        ["P1M2DT3S", new Duration(1, 2, 3, 0)],
        ["PT1H1M1S", new Duration(0, 0, 3661, 0)],
        ["PT1.5S", new Duration(0, 0, 1, 500000000)],
        ["PT-3.5S", new Duration(0, 0, -4, 500000000)],
        ["PT0,123456789S", new Duration(0, 0, 0, 123456789)]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(Duration value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Duration");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsDuration(Duration expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, Duration expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"Duration","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(_subject, "Duration");
    }

    [Fact]
    public void CanWrite_TrueForDuration()
    {
        _subject.CanWrite(new Duration(0, 0, 0, 0)).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForOtherTypes()
    {
        CanWrite(_subject);
    }
}
