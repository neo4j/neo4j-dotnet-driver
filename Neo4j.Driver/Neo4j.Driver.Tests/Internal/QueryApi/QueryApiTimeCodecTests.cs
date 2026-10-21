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

public class QueryApiTimeCodecTests
{
    private readonly QueryApiTimeCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new OffsetTime(13, 45, 30, 123456789, 0), "13:45:30.123456789Z"],
        [new OffsetTime(0, 0, 0, 0, 0), "00:00:00Z"],
        [new OffsetTime(0, 0, 0, 0, -64800), "00:00:00-18:00"],
        [new OffsetTime(23, 59, 59, 999999999, 64800), "23:59:59.999999999+18:00"],
        [new OffsetTime(13, 45, 30, 123456789, 3661), "13:45:30.123456789+01:01:01"],
        [new OffsetTime(13, 45, 30, 123456789, -3661), "13:45:30.123456789-01:01:01"],
        [new OffsetTime(0, 0, 0, 0, 60), "00:00:00+00:01"],
        [new OffsetTime(0, 0, 0, 0, -60), "00:00:00-00:01"]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["13:45+01:00", new OffsetTime(13, 45, 0, 0, 3600)],
        ["13:45:30+0100", new OffsetTime(13, 45, 30, 0, 3600)],
        ["00:00:00+00:00", new OffsetTime(0, 0, 0, 0, 0)],
        ["08:09:10.5Z", new OffsetTime(8, 9, 10, 500000000, 0)]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(OffsetTime value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Time");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsOffsetTime(OffsetTime expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, OffsetTime expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"Time","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Time");

    [Fact]
    public void CanWrite_TrueForOffsetTime() =>
        _subject.CanWrite(new OffsetTime(1, 2, 3, 4, 0)).Should().BeTrue();

    [Fact]
    public void CanWrite_FalseForOtherTypes() => CanWrite(_subject);
}
