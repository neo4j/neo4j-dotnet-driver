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

using System.Text.Json;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiCodecAssert;

#pragma warning disable CS0618 // IRelationship.Id/StartNodeId/EndNodeId are obsolete

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiRelationshipCodecTests
{
    private readonly QueryApiRelationshipCodec _subject = new();

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Relationship");

    [Fact]
    public void CanWrite_FalseForAllTypes() => CanWrite(_subject);

    [Fact]
    public void Write_Throws()
    {
        _subject
            .Invoking(s => s.Write(null, Mock.Of<IJsonValueEncoder>()))
            .Should()
            .Throw<System.NotSupportedException>();
    }

    [Fact]
    public void Read_EmptyRelationship_HasIdsTypeAndNoProperties()
    {
        using var doc = JsonDocument.Parse(
            """
            {
                "$type": "Relationship",
                "_value": {
                    "_element_id": "rel0",
                    "_start_node_element_id": "start0",
                    "_end_node_element_id": "end0",
                    "_type": "KNOWS",
                    "_properties": {}
                }
            }
            """);

        var relationship = (IRelationship)_subject.Read(doc.RootElement, Mock.Of<IJsonValueDecoder>())!;

        relationship.ElementId.Should().Be("rel0");
        relationship.StartNodeElementId.Should().Be("start0");
        relationship.EndNodeElementId.Should().Be("end0");
        relationship.Type.Should().Be("KNOWS");
        relationship.Properties.Should().BeEmpty();
        relationship.Id.Should().Be(0);
        relationship.StartNodeId.Should().Be(0);
        relationship.EndNodeId.Should().Be(0);
    }

    [Fact]
    public void Read_TypeAndProperties_DecodesEachPropertyValue()
    {
        using var doc = JsonDocument.Parse(
            """
            {
                "$type": "Relationship",
                "_value": {
                    "_element_id": "🔗",
                    "_start_node_element_id": "🅰",
                    "_end_node_element_id": "🅱",
                    "_type": "SINCE",
                    "_properties": {
                        "since": { "$type": "Integer", "_value": "999" },
                        "active": { "$type": "Boolean", "_value": "!active!" }
                    }
                }
            }
            """);

        var props = doc.RootElement.GetProperty("_value").GetProperty("_properties");
        var sinceElement = props.GetProperty("since");
        var activeElement = props.GetProperty("active");

        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == sinceElement.GetRawText())))
            .Returns(2020L);

        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == activeElement.GetRawText())))
            .Returns(true);

        var relationship = (IRelationship)_subject.Read(doc.RootElement, decoder.Object)!;

        relationship.ElementId.Should().Be("🔗");
        relationship.StartNodeElementId.Should().Be("🅰");
        relationship.EndNodeElementId.Should().Be("🅱");
        relationship.Type.Should().Be("SINCE");
        relationship.Properties.Should().HaveCount(2);
        relationship.Properties["since"].Should().Be(2020L);
        relationship.Properties["active"].Should().Be(true);
    }
}
