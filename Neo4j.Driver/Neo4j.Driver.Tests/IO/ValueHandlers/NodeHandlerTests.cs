﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Tests.IO.MessageHandlers;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.ValueHandlers
{
    public class NodeHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new NodeHandler();

        [Fact]
        public void ShouldThrowOnWrite()
        {
            var handler = HandlerUnderTest;

            var ex = Record.Exception(() =>
                handler.Write(Mock.Of<IPackStreamWriter>(),
                    new Node(0, new List<string> {"Label"}, new Dictionary<string, object>())));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldRead()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            WriteNode(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            VerifyWrittenNode(value);
        }

        [Fact]
        public void ShouldReadWhenInList()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteListHeader(1);
            WriteNode(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IList>().Which.Should().HaveCount(1);

            VerifyWrittenNode(value.Should().BeAssignableTo<IList>().Which[0]);
        }

        [Fact]
        public void ShouldReadWhenInMap()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteMapHeader(1);
            writer.Write("x");
            WriteNode(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IDictionary<string, object>>().Which.Should().HaveCount(1).And
                .ContainKey("x");

            VerifyWrittenNode(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
        }

        private static void WriteNode(IPackStreamWriter writer)
        {
            writer.WriteStructHeader(3, NodeHandler.Node);
            writer.Write(1);
            writer.Write(new List<string> {"Label1", "Label2"});
            writer.Write(new Dictionary<string, object>
            {
                {"prop1", "something"},
                {"prop2", 15},
                {"prop3", true}
            });
        }

        private static void VerifyWrittenNode(object value)
        {
            value.Should().NotBeNull();
            value.Should().BeOfType<Node>().Which.Id.Should().Be(1L);
            value.Should().BeOfType<Node>().Which.Labels.Should().Equal(new[] { "Label1", "Label2" });
            value.Should().BeOfType<Node>().Which.Properties.Should().HaveCount(3).And.Contain(new[]
            {
                new KeyValuePair<string, object>("prop1", "something"),
                new KeyValuePair<string, object>("prop2", 15L),
                new KeyValuePair<string, object>("prop3", true),
            });
        }
    }
}