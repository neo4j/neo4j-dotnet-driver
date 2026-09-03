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

internal record CypherTime : ICypherValue
{
    public required int Hour { get; init; }
    public required int Minute { get; init; }
    public required int Second { get; init; }
    public required int Nanosecond { get; init; }

    [JsonPropertyName("utc_offset_s")]
    public int? UtcOffsetS { get; init; }

    public CypherTime()
    {
    }

    [SetsRequiredMembers]
    internal CypherTime(LocalTime time)
    {
        Hour = time.Hour;
        Minute = time.Minute;
        Second = time.Second;
        Nanosecond = time.Nanosecond;
    }

    [SetsRequiredMembers]
    internal CypherTime(OffsetTime time)
    {
        Hour = time.Hour;
        Minute = time.Minute;
        Second = time.Second;
        Nanosecond = time.Nanosecond;
        UtcOffsetS = time.OffsetSeconds;
    }

    internal LocalTime ToLocalTime()
    {
        return new LocalTime(Hour, Minute, Second, Nanosecond);
    }

    internal OffsetTime ToOffsetTime()
    {
        return new OffsetTime(Hour, Minute, Second, Nanosecond, UtcOffsetS!.Value);
    }
}
