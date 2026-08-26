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

using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.TestKitBackend.Cypher;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Cypher;

// Testkit's decode_hook only unwraps a value that arrives as a {"name", "data"} envelope; a bare
// JSON number/string is left untouched. So a raw `id`/`elementId` on the wire silently becomes a
// plain int/str on the frontend instead of a CypherInt/CypherString, and the driver-side mapper
// tests (which assert against the mapper's C# object output, not the JSON it serializes to) can't
// see that. These tests exercise the actual wire bytes to catch that class of gap.
public class CypherGraphTypesWireFormatTests
{
    private readonly NativeToCypherMapper _mapper = new();

    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new CypherValueConverter(new Mock<ICypherValueTypeMap>().Object) }
        };
    }

    [Fact]
    public void Node_id_and_element_id_are_wire_tagged_as_cypher_int_and_cypher_string()
    {
        var node = new Mock<INode>();
        node.SetupGet(n => n.Id).Returns(1L);
        node.SetupGet(n => n.Labels).Returns(new List<string>());
        node.SetupGet(n => n.Properties).Returns(new Dictionary<string, object>());
        node.SetupGet(n => n.ElementId).Returns("element-id-1");

        var cypherNode = _mapper.Map(node.Object);

        var json = JsonSerializer.Serialize(cypherNode, Options());

        json.Should().Be(
            """{"name":"CypherNode","data":{"id":{"name":"CypherInt","data":{"value":1}},"labels":{"name":"CypherList","data":{"value":[]}},"props":{"name":"CypherMap","data":{"value":{}}},"elementId":{"name":"CypherString","data":{"value":"element-id-1"}}}}""");
    }

    [Fact]
    public void Relationship_id_fields_are_wire_tagged_as_cypher_int_and_cypher_string()
    {
        var relationship = new Mock<IRelationship>();
        relationship.SetupGet(r => r.Id).Returns(1L);
        relationship.SetupGet(r => r.StartNodeId).Returns(2L);
        relationship.SetupGet(r => r.EndNodeId).Returns(3L);
        relationship.SetupGet(r => r.Type).Returns("KNOWS");
        relationship.SetupGet(r => r.Properties).Returns(new Dictionary<string, object>());
        relationship.SetupGet(r => r.ElementId).Returns("rel-1");
        relationship.SetupGet(r => r.StartNodeElementId).Returns("node-2");
        relationship.SetupGet(r => r.EndNodeElementId).Returns("node-3");

        var cypherRelationship = _mapper.Map(relationship.Object);

        var json = JsonSerializer.Serialize(cypherRelationship, Options());

        json.Should().Be(
            """{"name":"CypherRelationship","data":{"id":{"name":"CypherInt","data":{"value":1}},"startNodeId":{"name":"CypherInt","data":{"value":2}},"endNodeId":{"name":"CypherInt","data":{"value":3}},"type":{"name":"CypherString","data":{"value":"KNOWS"}},"props":{"name":"CypherMap","data":{"value":{}}},"elementId":{"name":"CypherString","data":{"value":"rel-1"}},"startNodeElementId":{"name":"CypherString","data":{"value":"node-2"}},"endNodeElementId":{"name":"CypherString","data":{"value":"node-3"}}}}""");
    }
}
