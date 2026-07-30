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

using System.Text.Json.Serialization;

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal record CypherDateTime(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute,
    int Second,
    int Nanosecond,
    [property: JsonPropertyName("utc_offset_s")] int? UtcOffsetS = null,
    [property: JsonPropertyName("timezone_id")] string? TimezoneId = null) : ICypherValue
{
    internal CypherDateTime(ZonedDateTime zdt, int offsetSeconds, string? timezoneId = null)
        : this(zdt.Year, zdt.Month, zdt.Day, zdt.Hour, zdt.Minute, zdt.Second, zdt.Nanosecond, offsetSeconds, timezoneId)
    {
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
