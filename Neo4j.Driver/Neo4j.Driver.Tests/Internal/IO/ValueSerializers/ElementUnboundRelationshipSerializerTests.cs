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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers;
using Neo4j.Driver.Internal.Types;
using Xunit;

#pragma warning disable CS0618

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers;

public class ElementUnboundRelationshipSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new ElementUnboundRelationshipSerializer();

    [Fact]
    public void ShouldDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(3, UnboundRelationshipSerializer.UnboundRelationship);
        writer.Write(1);
        writer.Write("RELATES_TO");
        writer.Write(
            new Dictionary<string, object>
            {
                { "prop3", true }
            });

        writer.Write("r1");

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        Validate(value);
    }

    [Fact]
    public void ShouldDeserializeSpan()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(3, UnboundRelationshipSerializer.UnboundRelationship);
        writer.Write(1);
        writer.Write("RELATES_TO");
        writer.Write(
            new Dictionary<string, object>
            {
                { "prop3", true }
            });

        writer.Write("r1");

        var readerMachine = CreateSpanReader(writerMachine.GetOutput());
        var value = readerMachine.Read();

        Validate(value);
    }

    private static void Validate(object value)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<Relationship>().Which.Id.Should().Be(1L);
        value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(-1L);
        value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(-1L);
        value.Should().BeOfType<Relationship>().Which.Type.Should().Be("RELATES_TO");
        value.Should()
            .BeOfType<Relationship>()
            .Which.Properties.Should()
            .HaveCount(1)
            .And.Contain(new KeyValuePair<string, object>("prop3", true));

        value.Should().BeOfType<Relationship>().Which.ElementId.Should().Be("r1");
        value.Should().BeOfType<Relationship>().Which.StartNodeElementId.Should().Be("-1");
        value.Should().BeOfType<Relationship>().Which.EndNodeElementId.Should().Be("-1");
    }
}
