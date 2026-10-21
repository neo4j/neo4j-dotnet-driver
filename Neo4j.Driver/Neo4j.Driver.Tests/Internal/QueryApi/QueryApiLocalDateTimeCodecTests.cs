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

public class QueryApiLocalDateTimeCodecTests
{
    private readonly QueryApiLocalDateTimeCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new LocalDateTime(1, 1, 1, 0, 0, 0, 0), "0001-01-01T00:00:00"],
        [new LocalDateTime(44, 2, 29, 23, 59, 59, 999999999), "0044-02-29T23:59:59.999999999"],
        [new LocalDateTime(2026, 1, 27, 9, 40, 48, 1000), "2026-01-27T09:40:48.000001000"],
        [new LocalDateTime(9999, 12, 31, 23, 59, 59, 999999999), "9999-12-31T23:59:59.999999999"],
        [new LocalDateTime(25618, 12, 31, 23, 59, 59, 0), "+25618-12-31T23:59:59"],
        [new LocalDateTime(0, 12, 24, 22, 21, 23, 0), "0000-12-24T22:21:23"],
        [new LocalDateTime(-1, 7, 11, 13, 17, 19, 23), "-0001-07-11T13:17:19.000000023"],
        [new LocalDateTime(-25618, 12, 31, 23, 59, 59, 0), "-25618-12-31T23:59:59"]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["2024-02-29T13:45", new LocalDateTime(2024, 2, 29, 13, 45, 0, 0)],
        ["2024-02-29T13:45:30", new LocalDateTime(2024, 2, 29, 13, 45, 30, 0)],
        ["2024-02-29T13:45:30.5", new LocalDateTime(2024, 2, 29, 13, 45, 30, 500000000)],
        ["25618-12-31T23:59:59", new LocalDateTime(25618, 12, 31, 23, 59, 59, 0)]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(LocalDateTime value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("LocalDateTime");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsLocalDateTime(LocalDateTime expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, LocalDateTime expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"LocalDateTime","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "LocalDateTime");

    [Fact]
    public void CanWrite_TrueForLocalDateTime() =>
        _subject.CanWrite(new LocalDateTime(2024, 1, 1, 0, 0, 0, 0)).Should().BeTrue();

    [Fact]
    public void CanWrite_FalseForOtherTypes() => CanWrite(_subject);
}
