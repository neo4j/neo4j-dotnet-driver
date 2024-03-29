﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Tests.Internal.IO.Utils;
using Xunit;

#pragma warning disable CS0618

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers;

public class UnboundRelationshipSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new UnboundRelationshipSerializer();

    private new PackStreamWriterMachine CreateWriterMachine()
    {
        return CreateWriterMachine(BoltProtocolVersion.V4_0);
    }

    [Fact]
    public void ShouldDeserialize()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializeUnboundRelationship(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        VerifySerializedUnboundRelationship(value);
    }

    [Fact]
    public void ShouldDeserializeWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializeUnboundRelationship(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList>().Which.Should().HaveCount(1);

        VerifySerializedUnboundRelationship(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializeUnboundRelationship(writer);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var value = readerMachine.Reader().Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedUnboundRelationship(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }

    [Fact]
    public void ShouldDeserializeSpan()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        SerializeUnboundRelationship(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        VerifySerializedUnboundRelationship(value);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInList()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteListHeader(1);
        SerializeUnboundRelationship(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        value.Should().NotBeNull();
        value.Should().BeAssignableTo<IList>().Which.Should().HaveCount(1);

        VerifySerializedUnboundRelationship(value.Should().BeAssignableTo<IList>().Which[0]);
    }

    [Fact]
    public void ShouldDeserializeSpanWhenInMap()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteMapHeader(1);
        writer.Write("x");
        SerializeUnboundRelationship(writer);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        value.Should().NotBeNull();
        value.Should()
            .BeAssignableTo<IDictionary<string, object>>()
            .Which.Should()
            .HaveCount(1)
            .And
            .ContainKey("x");

        VerifySerializedUnboundRelationship(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
    }
        
    private static void SerializeUnboundRelationship(PackStreamWriter writer)
    {
        writer.WriteStructHeader(3, UnboundRelationshipSerializer.UnboundRelationship);
        writer.Write(1);
        writer.Write("RELATES_TO");
        writer.Write(
            new Dictionary<string, object>
            {
                { "prop1", "something" },
                { "prop2", 2.0 },
                { "prop3", false }
            });
    }

    private static void VerifySerializedUnboundRelationship(object value)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<Relationship>().Which.Id.Should().Be(1L);
        value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(-1L);
        value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(-1L);
        value.Should().BeOfType<Relationship>().Which.Type.Should().Be("RELATES_TO");
        value.Should()
            .BeOfType<Relationship>()
            .Which.Properties.Should()
            .HaveCount(3)
            .And.Contain(
                new[]
                {
                    new KeyValuePair<string, object>("prop1", "something"),
                    new KeyValuePair<string, object>("prop2", 2.0),
                    new KeyValuePair<string, object>("prop3", false)
                });
    }
}