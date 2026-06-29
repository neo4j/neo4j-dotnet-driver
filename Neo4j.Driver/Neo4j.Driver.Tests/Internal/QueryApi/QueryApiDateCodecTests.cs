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

/// <summary>
/// Round-trips the HTTP Query API <c>Date</c> type through <see cref="QueryApiDateCodec"/>. The wire value is an
/// ISO-8601 date string; years 0..9999 are zero-padded to four digits, negative years carry a sign, and years
/// above 9999 are prefixed with <c>+</c> (the expanded-year representation the API requires).
/// </summary>
public class QueryApiDateCodecTests
{
    private readonly QueryApiDateCodec _subject = new();

    public static IEnumerable<object[]> DateCases() =>
    [
        [new LocalDate(1970, 1, 1), "1970-01-01"],
        [new LocalDate(2024, 2, 29), "2024-02-29"],
        [new LocalDate(1, 1, 1), "0001-01-01"],
        [new LocalDate(9999, 12, 31), "9999-12-31"],
        [new LocalDate(0, 1, 1), "0000-01-01"],
        [new LocalDate(-1, 1, 1), "-0001-01-01"],
        [new LocalDate(-200, 1, 1), "-0200-01-01"],
        [new LocalDate(-10000, 1, 1), "-10000-01-01"],
        [new LocalDate(10000, 1, 1), "+10000-01-01"],
        [new LocalDate(999999, 12, 31), "+999999-12-31"],
        [new LocalDate(-40000, 2, 29), "-40000-02-29"]
    ];

    [Theory]
    [MemberData(nameof(DateCases))]
    public void Write_ReturnsTypedEnvelope(LocalDate value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("Date");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(DateCases))]
    public void Read_ReturnsLocalDate(LocalDate expected, string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"Date","_value":"{{wireValue}}"}""");

        var result = _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());

        result.Should().Be(expected);
    }

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Date");

    [Fact]
    public void CanWrite_TrueForLocalDate() =>
        _subject.CanWrite(new LocalDate(2024, 1, 1)).Should().BeTrue();

    [Fact]
    public void CanWrite_FalseForOtherTypes() => CanWrite(_subject);
}
