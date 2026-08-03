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
            IVector vector => new CypherVector(vector),

            UnsupportedType unsupported => new CypherUnsupportedType(
                unsupported.Name,
                unsupported.MinimumProtocolVersion,
                unsupported.Message),

            INode node => new CypherNode(
                node.Id,
                new CypherList([.. node.Labels.Select(l => Map(l))]),
                new CypherMap(node.Properties.ToDictionary(kv => kv.Key, kv => Map(kv.Value))),
                node.ElementId),

            IRelationship relationship => new CypherRelationship(
                relationship.Id,
                relationship.StartNodeId,
                relationship.EndNodeId,
                relationship.Type,
                new CypherMap(relationship.Properties.ToDictionary(kv => kv.Key, kv => Map(kv.Value))),
                relationship.ElementId,
                relationship.StartNodeElementId,
                relationship.EndNodeElementId),

            IPath path => new CypherPath(
                new CypherList([.. path.Nodes.Select(n => Map(n))]),
                new CypherList([.. path.Relationships.Select(r => Map(r))])),

            _ => throw new NotSupportedException($"No cypher mapping for native type {value.GetType().Name}")
        };
    }
}
