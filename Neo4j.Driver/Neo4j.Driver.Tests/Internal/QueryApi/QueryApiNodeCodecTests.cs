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

#pragma warning disable CS0618 // INode.Id is obsolete; asserted here to document the HTTP polyfill

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiNodeCodecTests
{
    private readonly QueryApiNodeCodec _subject = new();

    [Fact]
    public void CanRead_CorrectTypes() => CanRead(_subject, "Node");

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
    public void Read_EmptyNode_HasElementIdEmptyLabelsAndProperties()
    {
        using var doc = JsonDocument.Parse(
            """
            {
                "$type": "Node",
                "_value": {
                    "_element_id": "node0",
                    "_labels": [],
                    "_properties": {}
                }
            }
            """);

        var node = (INode)_subject.Read(doc.RootElement, Mock.Of<IJsonValueDecoder>())!;

        node.ElementId.Should().Be("node0");
        node.Labels.Should().BeEmpty();
        node.Properties.Should().BeEmpty();
        node.Id.Should().Be(0);
    }

    [Fact]
    public void Read_LabelsAndProperties_DecodesEachPropertyValue()
    {
        using var doc = JsonDocument.Parse(
            """
            {
                "$type": "Node",
                "_value": {
                    "_element_id": "🪪",
                    "_labels": ["Person", "Admin"],
                    "_properties": {
                        "name": { "$type": "String", "_value": "!name!" },
                        "age": { "$type": "Integer", "_value": "999" }
                    }
                }
            }
            """);

        var props = doc.RootElement.GetProperty("_value").GetProperty("_properties");
        var nameElement = props.GetProperty("name");
        var ageElement = props.GetProperty("age");

        var decoder = new Mock<IJsonValueDecoder>();
        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == nameElement.GetRawText())))
            .Returns("Alice");

        decoder.Setup(d => d.Decode(It.Is<JsonElement>(e => e.GetRawText() == ageElement.GetRawText()))).Returns(42L);

        var node = (INode)_subject.Read(doc.RootElement, decoder.Object)!;

        node.ElementId.Should().Be("🪪");
        node.Labels.Should().ContainInOrder("Person", "Admin");
        node.Properties.Should().HaveCount(2);
        node.Properties["name"].Should().Be("Alice");
        node.Properties["age"].Should().Be(42L);
    }
}
