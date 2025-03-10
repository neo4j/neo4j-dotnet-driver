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

using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers;
using Neo4j.Driver.Internal.Types;
using Xunit;

#pragma warning disable CS0618

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers;

public class PathSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new PathSerializer();

    internal override IEnumerable<IPackStreamSerializer> SerializersNeeded =>
        new IPackStreamSerializer[] { new NodeSerializer(), new UnboundRelationshipSerializer() };

    [Fact]
    public void ShouldDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializePath(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        VerifySerializedPath(value);
    }

    [Fact]
    public void ShouldDeserializeSpan()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializePath(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        VerifySerializedPath(value);
    }

    [Fact]
    public void ShouldDeserializeReverse()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializePath(writer, true);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        VerifySerializedPathReverse(value);
    }

    [Fact]
    public void ShouldDeserializeSpanReverse()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializePath(writer, true);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        VerifySerializedPathReverse(value);
    }

    [Fact]
    public void ShouldDeserializeWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializePath(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList>().Which.Count.Should().Be(1);

        VerifySerializedPath(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializePath(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList>().Which.Count.Should().Be(1);

        VerifySerializedPath(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializePath(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedPath(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializePath(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedPath(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }

    private static void SerializePath(PackStreamWriter writer, bool reverse = false)
    {
        writer.WriteStructHeader(3, PathSerializer.Path);

        writer.WriteListHeader(3);
        for (var i = 0; i < 3; i++)
        {
            SerializeNode(writer, i + 1);
        }

        writer.WriteListHeader(3);
        SerializeUnboundedRelationship(writer, 4);
        SerializeUnboundedRelationship(writer, 5);
        SerializeUnboundedRelationship(writer, 6);

        writer.WriteListHeader(6);
        writer.Write(1 * (reverse ? -1 : 1));
        writer.Write(1);
        writer.Write(2 * (reverse ? -1 : 1));
        writer.Write(2);
        writer.Write(3 * (reverse ? -1 : 1));
        writer.Write(0);
    }

    private static void SerializeNode(PackStreamWriter writer, int id)
    {
        writer.WriteStructHeader(3, NodeSerializer.Node);
        writer.Write(id);
        writer.Write(new List<string> { $"Label{id}" });
        writer.Write(
            new Dictionary<string, object>
            {
                { "nProp1", $"something{id}" }
            });
    }

    private static void SerializeUnboundedRelationship(PackStreamWriter writer, int id)
    {
        writer.WriteStructHeader(3, UnboundRelationshipSerializer.UnboundRelationship);
        writer.Write(id);
        writer.Write($"RELATES_TO_{id}");
        writer.Write(
            new Dictionary<string, object>
            {
                { "rProp1", $"something{id}" }
            });
    }

    private static void VerifySerializedPath(object value)
    {
        value.Should().NotBeNull();

        value.Should().BeOfType<Path>().Which.Nodes.Should().HaveCount(4);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[0], 1);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[1], 2);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[2], 3);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[3], 1);

        value.Should().BeOfType<Path>().Which.Relationships.Should().HaveCount(3);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[0], 4, 1, 2);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[1], 5, 2, 3);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[2], 6, 3, 1);
    }

    private static void VerifySerializedPathReverse(object value)
    {
        value.Should().NotBeNull();

        value.Should().BeOfType<Path>().Which.Nodes.Should().HaveCount(4);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[0], 1);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[1], 2);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[2], 3);
        VerifySerializedNode(value.Should().BeOfType<Path>().Which.Nodes[3], 1);

        value.Should().BeOfType<Path>().Which.Relationships.Should().HaveCount(3);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[0], 4, 2, 1);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[1], 5, 3, 2);
        VerifySerializedUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[2], 6, 1, 3);
    }

    private static void VerifySerializedNode(object value, int expectedId)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<Node>().Which.Id.Should().Be(expectedId);
        value.Should().BeOfType<Node>().Which.Labels.Should().HaveCount(1).And.Contain($"Label{expectedId}");
        value.Should()
            .BeOfType<Node>()
            .Which.Properties.Should()
            .HaveCount(1)
            .And
            .Contain(new KeyValuePair<string, object>("nProp1", $"something{expectedId}"));
    }

    private static void VerifySerializedUnboundedRelationship(
        object value,
        int expectedId,
        int expectedStartNodeId,
        int expectedEndNodeId)
    {
        value.Should().BeOfType<Relationship>().Which.Id.Should().Be(expectedId);
        value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(expectedStartNodeId);
        value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(expectedEndNodeId);
        value.Should().BeOfType<Relationship>().Which.Type.Should().Be($"RELATES_TO_{expectedId}");
        value.Should()
            .BeOfType<Relationship>()
            .Which.Properties.Should()
            .HaveCount(1)
            .And
            .Contain(new KeyValuePair<string, object>("rProp1", $"something{expectedId}"));
    }
}
