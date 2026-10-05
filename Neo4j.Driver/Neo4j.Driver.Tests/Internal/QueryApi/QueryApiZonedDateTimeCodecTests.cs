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

public class QueryApiZonedDateTimeCodecTests
{
    private readonly QueryApiZonedDateTimeCodec _subject = new();

    public static IEnumerable<object[]> RoundTripCases() =>
    [
        [
            new ZonedDateTime(2025, 1, 1, 13, 45, 30, 123456789, Zone.Of("Europe/Berlin")),
            "2025-01-01T13:45:30.123456789+01:00[Europe/Berlin]"
        ],
        [
            new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of("Africa/Abidjan")),
            "2025-01-01T00:00:00Z[Africa/Abidjan]"
        ],
        [
            new ZonedDateTime(2025, 1, 1, 13, 45, 30, 123456789, Zone.Of("Pacific/Niue")),
            "2025-01-01T13:45:30.123456789-11:00[Pacific/Niue]"
        ],
        [
            new ZonedDateTime(2025, 1, 1, 13, 45, 30, 123456789, Zone.Of("Pacific/Kiritimati")),
            "2025-01-01T13:45:30.123456789+14:00[Pacific/Kiritimati]"
        ]
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Write_ReturnsTypedEnvelope(ZonedDateTime value, string expectedValue)
    {
        var result = _subject.Write(value, Mock.Of<IJsonValueEncoder>())!;

        result["$type"]!.GetValue<string>().Should().Be("ZonedDateTime");
        result["_value"]!.GetValue<string>().Should().Be(expectedValue);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ReturnsZonedDateTime(ZonedDateTime expected, string wireValue)
    {
        Read(wireValue).Should().Be(expected);
    }

    [Fact]
    public void Read_RecomputesOffsetFromZone_WhenWireOffsetDisagrees()
    {
        // Server may emit an offset that disagrees with the zone's rules; the driver
        // should resolve the UTC instant and recompute the local time + offset from the zone.
        Read("2025-01-01T11:45:30.123456789-01:00[Europe/Berlin]")
            .Should()
            .Be(new ZonedDateTime(2025, 1, 1, 13, 45, 30, 123456789, Zone.Of("Europe/Berlin")));
    }

    private object? Read(string wireValue)
    {
        using var document = JsonDocument.Parse($$"""{"$type":"ZonedDateTime","_value":"{{wireValue}}"}""");
        return _subject.Read(document.RootElement, Mock.Of<IJsonValueDecoder>());
    }

    [Fact]
    public void CanRead_CorrectTypes()
    {
        CanRead(_subject, "ZonedDateTime");
    }

    [Fact]
    public void CanWrite_TrueForNamedZoneDateTime()
    {
        _subject.CanWrite(new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of("Africa/Abidjan"))).Should().BeTrue();
    }

    [Fact]
    public void CanWrite_FalseForOffsetDateTime()
    {
        _subject.CanWrite(new ZonedDateTime(2025, 1, 1, 0, 0, 0, 0, Zone.Of(0))).Should().BeFalse();
    }

    [Fact]
    public void CanWrite_FalseForOtherTypes()
    {
        CanWrite(_subject);
    }
}
