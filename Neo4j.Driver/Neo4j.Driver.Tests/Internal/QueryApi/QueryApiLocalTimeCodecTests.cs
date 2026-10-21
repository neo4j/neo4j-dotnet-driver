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

public class QueryApiLocalTimeCodecTests
{
    private readonly QueryApiLocalTimeCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new LocalTime(0, 0, 0, 0), "00:00:00"],
        [new LocalTime(1, 2, 3, 0), "01:02:03"],
        [new LocalTime(13, 45, 30, 123456789), "13:45:30.123456789"],
        [new LocalTime(12, 30, 0, 500000000), "12:30:00.500000000"],
        [new LocalTime(23, 59, 59, 999999999), "23:59:59.999999999"]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["13:45", new LocalTime(13, 45, 0, 0)],
        ["13:45:30", new LocalTime(13, 45, 30, 0)],
        ["08:09:10.5", new LocalTime(8, 9, 10, 500000000)],
        ["08:09:10.000000001", new LocalTime(8, 9, 10, 1)]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(LocalTime value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("LocalTime");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsLocalTime(LocalTime expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, LocalTime expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"LocalTime","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "LocalTime");

    [Fact]
    public void CanWrite_TrueForLocalTime() =>
        _subject.CanWrite(new LocalTime(1, 2, 3, 4)).Should().BeTrue();

    [Fact]
    public void CanWrite_FalseForOtherTypes() => CanWrite(_subject);
}
