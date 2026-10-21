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

public class QueryApiOffsetDateTimeCodecTests
{
    private readonly QueryApiOffsetDateTimeCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [new ZonedDateTime(2025, 1, 1, 13, 45, 30, 123456789, Zone.Of(3600)), "2025-01-01T13:45:30.123456789+01:00"],
        [new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of(0)), "2025-01-01T00:00:00Z"],
        [new ZonedDateTime(2025, 6, 15, 23, 59, 59, 999999999, Zone.Of(-39600)), "2025-06-15T23:59:59.999999999-11:00"],
        [new ZonedDateTime(2025, 1, 1, 12, 0, 0, 0, Zone.Of(50400)), "2025-01-01T12:00:00+14:00"],
        [new ZonedDateTime(2025, 1, 1, 13, 45, 30, 0, Zone.Of(3661)), "2025-01-01T13:45:30+01:01:01"]
    ];

    public static IEnumerable<object[]> ReadOnlyCases() =>
    [
        ["2025-01-01T13:45+01:00", new ZonedDateTime(2025, 1, 1, 13, 45, 0, 0, Zone.Of(3600))],
        ["2025-01-01T13:45:30+0100", new ZonedDateTime(2025, 1, 1, 13, 45, 30, 0, Zone.Of(3600))],
        ["2025-01-01T00:00:00+00:00", new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of(0))],
        ["2025-01-01T08:09:10.5Z", new ZonedDateTime(2025, 1, 1, 8, 9, 10, 500000000, Zone.Of(0))]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(ZonedDateTime value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("OffsetDateTime");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsZonedDateTime(ZonedDateTime expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public void Read_AcceptsLenientForms(string wireValue, ZonedDateTime expected)
    {
        Read(wireValue).Should().Be(expected);
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"OffsetDateTime","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(_subject, "OffsetDateTime");
    }

    [Fact]
    public void CanWrite_TrueForOffsetZonedDateTime()
    {
        _subject
            .CanWrite(new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of(0)))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForNamedZoneDateTime()
    {
        _subject
            .CanWrite(new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of("Africa/Abidjan")))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CanWrite_FalseForOtherTypes()
    {
        CanWrite(_subject);
    }
}
