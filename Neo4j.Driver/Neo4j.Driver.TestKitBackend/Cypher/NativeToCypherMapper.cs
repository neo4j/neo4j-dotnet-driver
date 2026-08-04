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

#pragma warning disable CS0618 // Id/StartNodeId/EndNodeId are obsolete but still part of the wire contract.

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal interface INativeToCypherMapper
{
    ICypherValue Map(object? value);
}

internal class NativeToCypherMapper : INativeToCypherMapper
{
    public ICypherValue Map(object? value)
    {
        return value switch
        {
            null => new CypherNull(),
            bool b => new CypherBool(b),
            long l => new CypherInt(l),
            double d => new CypherFloat(d),
            string s => new CypherString(s),
            List<object> list => new CypherList([..list.Select(Map)]),
            Dictionary<string, object> map => new CypherMap(map.ToDictionary(kv => kv.Key, kv => Map(kv.Value))),
            Guid guid => new CypherUUID(guid),
            ZonedDateTime { Zone: ZoneOffset offset } zdt => new CypherDateTime(zdt, offset.OffsetSeconds),
            ZonedDateTime { Zone: ZoneId zoneId } zdt => new CypherDateTime(zdt, zdt.OffsetSeconds, zoneId.Id),
            LocalDateTime localDateTime => new CypherDateTime(localDateTime),
            LocalDate date => new CypherDate(date),
            OffsetTime offsetTime => new CypherTime(offsetTime),
            LocalTime time => new CypherTime(time),
            Duration duration => new CypherDuration(duration),
            byte[] bytes => new CypherBytes(bytes),
            Point point => new CypherPoint(point),
            IVector vector => new CypherVector(vector),

            UnsupportedType unsupported => new CypherUnsupportedType(
                unsupported.Name,
                unsupported.MinimumProtocolVersion,
                unsupported.Message),

            INode node => new CypherNode(
                new CypherInt(node.Id),
                new CypherList([.. node.Labels.Select(l => Map(l))]),
                new CypherMap(node.Properties.ToDictionary(kv => kv.Key, kv => Map(kv.Value))),
                new CypherString(node.ElementId)),

            IRelationship relationship => new CypherRelationship(
                new CypherInt(relationship.Id),
                new CypherInt(relationship.StartNodeId),
                new CypherInt(relationship.EndNodeId),
                relationship.Type,
                new CypherMap(relationship.Properties.ToDictionary(kv => kv.Key, kv => Map(kv.Value))),
                new CypherString(relationship.ElementId),
                new CypherString(relationship.StartNodeElementId),
                new CypherString(relationship.EndNodeElementId)),

            IPath path => new CypherPath(
                new CypherList([.. path.Nodes.Select(n => Map(n))]),
                new CypherList([.. path.Relationships.Select(r => Map(r))])),

            _ => throw new NotSupportedException($"No cypher mapping for native type {value.GetType().Name}")
        };
    }
}
