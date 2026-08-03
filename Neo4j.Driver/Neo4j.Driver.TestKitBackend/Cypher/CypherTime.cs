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

internal record CypherTime(
    int Hour,
    int Minute,
    int Second,
    int Nanosecond,

    [property: JsonPropertyName("utc_offset_s")] 
    int? UtcOffsetS = null) : ICypherValue
{
    internal CypherTime(LocalTime time)
        : this(time.Hour, time.Minute, time.Second, time.Nanosecond)
    {
    }

    internal CypherTime(OffsetTime time)
        : this(time.Hour, time.Minute, time.Second, time.Nanosecond, time.OffsetSeconds)
    {
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
