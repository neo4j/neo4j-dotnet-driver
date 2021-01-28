// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers
{
    public class RelationshipSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new RelationshipSerializer();

        [Fact]
        public void ShouldThrowOnSerialize()
        {
            var handler = SerializerUnderTest;

            var ex = Record.Exception(() =>
                handler.Serialize(Mock.Of<IPackStreamWriter>(),
                    new Relationship(0, 1, 2, "RELATES_TO", new Dictionary<string, object>())));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldDeserialize()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            SerializeRelationship(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            VerifySerializedRelationship(value);
        }

        [Fact]
        public void ShouldDeserializeWhenInList()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteListHeader(1);
            SerializeRelationship(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IList>().Which.Should().HaveCount(1);

            VerifySerializedRelationship(value.Should().BeAssignableTo<IList>().Which[0]);
        }

        [Fact]
        public void ShouldDeserializeWhenInMap()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteMapHeader(1);
            writer.Write("x");
            SerializeRelationship(writer);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeAssignableTo<IDictionary<string, object>>().Which.Should().HaveCount(1).And
                .ContainKey("x");

            VerifySerializedRelationship(value.Should().BeAssignableTo<IDictionary>().Which["x"]);
        }

        private static void SerializeRelationship(IPackStreamWriter writer)
        {
            writer.WriteStructHeader(5, RelationshipSerializer.Relationship);
            writer.Write(1);
            writer.Write(2);
            writer.Write(3);
            writer.Write("RELATES_TO");
            writer.Write(new Dictionary<string, object>
            {
                {"prop1", "something"},
                {"prop2", 2.0},
                {"prop3", false}
            });
        }

        private static void VerifySerializedRelationship(object value)
        {
            value.Should().NotBeNull();
            value.Should().BeOfType<Relationship>().Which.Id.Should().Be(1L);
            value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(2L);
            value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(3L);
            value.Should().BeOfType<Relationship>().Which.Type.Should().Be("RELATES_TO");
            value.Should().BeOfType<Relationship>().Which.Properties.Should().HaveCount(3).And.Contain(new[]
            {
                new KeyValuePair<string, object>("prop1", "something"),
                new KeyValuePair<string, object>("prop2", 2.0),
                new KeyValuePair<string, object>("prop3", false),
            });
        }
    }
}