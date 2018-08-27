// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueHandlers;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Tests.IO.MessageHandlers;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.ValueHandlers
{
    public class PathHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new PathHandler();

        internal override IEnumerable<IPackStreamStructHandler> HandlersNeeded =>
            new IPackStreamStructHandler[] {new NodeHandler(), new UnboundRelationshipHandler()};

        [Fact]
        public void ShouldThrowOnWrite()
        {
            var handler = HandlerUnderTest;

            var ex = Record.Exception(() =>
                handler.Write(Mock.Of<IPackStreamWriter>(),
                    new Path(new List<ISegment>(), new List<INode>(), new List<IRelationship>())));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldRead()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            WritePath(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            VerifyWrittenPath(value);
        }

        [Fact]
        public void ShouldReadReverse()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            WritePath(writer, true);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            VerifyWrittenPathReverse(value);
        }

        [Fact]
        public void ShouldReadWhenInList()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteListHeader(1);
            WritePath(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IList>().Which.Should().HaveCount(1);

            VerifyWrittenPath(value.Should().BeAssignableTo<IList>().Which[0]);
        }

        [Fact]
        public void ShouldReadWhenInMap()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteMapHeader(1);
            writer.Write("x");
            WritePath(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IDictionary<string, object>>().Which.Should().HaveCount(1).And
                .ContainKey("x");

            VerifyWrittenPath(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
        }

        private static void WritePath(IPackStreamWriter writer, bool reverse = false)
        {
            writer.WriteStructHeader(3, PathHandler.Path);
            
            writer.WriteListHeader(3);
            for (var i = 0; i < 3; i++)
            {
                WriteNode(writer, i + 1);
            }

            writer.WriteListHeader(3);
            WriteUnboundedRelationship(writer, 4);
            WriteUnboundedRelationship(writer, 5);
            WriteUnboundedRelationship(writer, 6);

            writer.WriteListHeader(6);
            writer.Write(1 * (reverse ? -1 : 1));
            writer.Write(1);
            writer.Write(2 * (reverse ? -1 : 1));
            writer.Write(2);
            writer.Write(3 * (reverse ? -1 : 1));
            writer.Write(0);
        }

        private static void WriteNode(IPackStreamWriter writer, int id)
        {
            writer.WriteStructHeader(3, NodeHandler.Node);
            writer.Write(id);
            writer.Write(new List<string> {$"Label{id}"});
            writer.Write(new Dictionary<string, object>
            {
                {"nProp1", $"something{id}"}
            });
        }

        private static void WriteUnboundedRelationship(IPackStreamWriter writer, int id)
        {
            writer.WriteStructHeader(3, UnboundRelationshipHandler.UnboundRelationship);
            writer.Write(id);
            writer.Write($"RELATES_TO_{id}");
            writer.Write(new Dictionary<string, object>
            {
                {"rProp1", $"something{id}"}
            });
        }

        private static void VerifyWrittenPath(object value)
        {
            value.Should().NotBeNull();

            value.Should().BeOfType<Path>().Which.Nodes.Should().HaveCount(4);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[0], 1);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[1], 2);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[2], 3);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[3], 1);

            value.Should().BeOfType<Path>().Which.Relationships.Should().HaveCount(3);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[0], 4, 1, 2);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[1], 5, 2, 3);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[2], 6, 3, 1);
        }

        private static void VerifyWrittenPathReverse(object value)
        {
            value.Should().NotBeNull();

            value.Should().BeOfType<Path>().Which.Nodes.Should().HaveCount(4);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[0], 1);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[1], 2);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[2], 3);
            VerifyWrittenNode(value.Should().BeOfType<Path>().Which.Nodes[3], 1);

            value.Should().BeOfType<Path>().Which.Relationships.Should().HaveCount(3);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[0], 4, 2, 1);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[1], 5, 3, 2);
            VerifyWrittenUnboundedRelationship(value.Should().BeOfType<Path>().Which.Relationships[2], 6, 1, 3);
        }

        private static void VerifyWrittenNode(object value, int expectedId)
        {
            value.Should().NotBeNull();
            value.Should().BeOfType<Node>().Which.Id.Should().Be(expectedId);
            value.Should().BeOfType<Node>().Which.Labels.Should().HaveCount(1).And.Contain($"Label{expectedId}");
            value.Should().BeOfType<Node>().Which.Properties.Should().HaveCount(1).And
                .Contain(new KeyValuePair<string, object>("nProp1", $"something{expectedId}"));
        }

        private static void VerifyWrittenUnboundedRelationship(object value, int expectedId, int expectedStartNodeId, int expectedEndNodeId)
        {
            value.Should().BeOfType<Relationship>().Which.Id.Should().Be(expectedId);
            value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(expectedStartNodeId);
            value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(expectedEndNodeId);
            value.Should().BeOfType<Relationship>().Which.Type.Should().Be($"RELATES_TO_{expectedId}");
            value.Should().BeOfType<Relationship>().Which.Properties.Should().HaveCount(1).And
                .Contain(new KeyValuePair<string, object>("rProp1", $"something{expectedId}"));
        }

    }
}