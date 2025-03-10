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

public class NodeSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new NodeSerializer();

    [Fact]
    public void ShouldDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializeNode(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        VerifySerializedNode(value);
    }

    [Fact]
    public void ShouldDeserializeWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializeNode(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList<object>>().Which.Should().HaveCount(1);

        VerifySerializedNode(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializeNode(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedNode(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }

    private static void SerializeNode(PackStreamWriter writer)
    {
        writer.WriteStructHeader(3, NodeSerializer.Node);
        writer.Write(1);
        writer.Write(new List<string> { "Label1", "Label2" });
        writer.Write(
            new Dictionary<string, object>
            {
                { "prop1", "something" },
                { "prop2", 15 },
                { "prop3", true }
            });
    }

    private static void VerifySerializedNode(object value)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<Node>().Which.Id.Should().Be(1L);
        value.Should().BeOfType<Node>().Which.Labels.Should().Equal("Label1", "Label2");
        value.Should()
            .BeOfType<Node>()
            .Which.Properties.Should()
            .HaveCount(3)
            .And.Contain(
                new KeyValuePair<string, object>("prop1", "something"),
                new KeyValuePair<string, object>("prop2", 15L),
                new KeyValuePair<string, object>("prop3", true));
    }

    [Fact]
    public void ShouldDeserializeSpan()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializeNode(writer);

        var readerMachine = CreateSpanReader(writerMachine.GetOutput());
        var value = readerMachine.Read();

        VerifySerializedNode(value);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializeNode(writer);

        var readerMachine = CreateSpanReader(writerMachine.GetOutput());
        var value = readerMachine.Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList>().Which.Count.Should().Be(1);

        VerifySerializedNode(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializeNode(writer);

        var readerMachine = CreateSpanReader(writerMachine.GetOutput());
        var value = readerMachine.Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedNode(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }
}
