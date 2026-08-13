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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal record CypherDateTime : ICypherValue
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int Day { get; init; }
    public required int Hour { get; init; }
    public required int Minute { get; init; }
    public required int Second { get; init; }
    public required int Nanosecond { get; init; }

    [JsonPropertyName("utc_offset_s")]
    public int? UtcOffsetS { get; init; }

    [JsonPropertyName("timezone_id")]
    public string? TimezoneId { get; init; }

    public CypherDateTime()
    {
    }

    [SetsRequiredMembers]
    internal CypherDateTime(ZonedDateTime zdt, int offsetSeconds, string? timezoneId = null)
    {
        Year = zdt.Year;
        Month = zdt.Month;
        Day = zdt.Day;
        Hour = zdt.Hour;
        Minute = zdt.Minute;
        Second = zdt.Second;
        Nanosecond = zdt.Nanosecond;
        UtcOffsetS = offsetSeconds;
        TimezoneId = timezoneId;
    }

    [SetsRequiredMembers]
    internal CypherDateTime(LocalDateTime ldt)
    {
        Year = ldt.Year;
        Month = ldt.Month;
        Day = ldt.Day;
        Hour = ldt.Hour;
        Minute = ldt.Minute;
        Second = ldt.Second;
        Nanosecond = ldt.Nanosecond;
    }

    internal LocalDateTime ToLocalDateTime()
    {
        return new LocalDateTime(Year, Month, Day, Hour, Minute, Second, Nanosecond);
    }

    internal ZonedDateTime ToZonedDateTime(int offset)
    {
        return ToZonedDateTime(Zone.Of(offset));
    }

    internal ZonedDateTime ToZonedDateTime(string zoneId)
    {
        return ToZonedDateTime(Zone.Of(zoneId));
    }

    private ZonedDateTime ToZonedDateTime(Zone zone)
    {
        return new ZonedDateTime(Year, Month, Day, Hour, Minute, Second, Nanosecond, zone);
    }
}
