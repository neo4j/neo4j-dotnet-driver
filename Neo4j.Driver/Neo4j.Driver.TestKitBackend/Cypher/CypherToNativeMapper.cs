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

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal interface ICypherToNativeMapper
{
    object? Map(ICypherValue value);

    Dictionary<string, object> Map(Dictionary<string, ICypherValue>? parameters);
}

internal class CypherToNativeMapper : ICypherToNativeMapper
{
    public object? Map(ICypherValue value)
    {
        return value switch
        {
            CypherNull => null,
            CypherBool b => b.Value,
            CypherInt i => i.Value,
            CypherFloat f => f.Value,
            CypherString s => s.Value,
            CypherList l => l.Value.Select(Map).ToList(),
            CypherMap m => m.Value.ToDictionary(kv => kv.Key, kv => Map(kv.Value)!),
            CypherUUID u => u.Value,
            CypherDateTime { TimezoneId: null, UtcOffsetS: not null } dt => dt.ToZonedDateTime(dt.UtcOffsetS.Value),
            CypherDateTime { TimezoneId: { } timezoneId } dt => dt.ToZonedDateTime(timezoneId),
            CypherDateTime dt => dt.ToLocalDateTime(),
            CypherDate d => d.ToLocalDate(),
            CypherTime { UtcOffsetS: { } } t => t.ToOffsetTime(),
            CypherTime t => t.ToLocalTime(),
            CypherDuration d => d.ToDuration(),
            CypherBytes b => b.ToBytes(),
            CypherPoint p => p.ToPoint(),
            CypherVector v => v.ToVector(),
            _ => throw new NotSupportedException($"No native mapping for cypher type {value.GetType().Name}")
        };
    }

    public Dictionary<string, object> Map(Dictionary<string, ICypherValue>? parameters)
    {
        return parameters?.ToDictionary(kv => kv.Key, kv => Map(kv.Value)!) ?? [];
    }
}
