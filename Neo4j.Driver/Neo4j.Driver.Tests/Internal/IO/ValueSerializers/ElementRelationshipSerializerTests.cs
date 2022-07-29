// Copyright (c) 2002-2022 "Neo4j,"
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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers
{
    public class ElementRelationshipSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new ElementRelationshipSerializer();

        [Fact]
        public void ShouldDeserialize()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(3, ElementRelationshipSerializer.Relationship);
            writer.Write(1);
            writer.Write(2);
            writer.Write(3);
            writer.Write("RELATES_TO");
            writer.Write(new Dictionary<string, object>
            {
                {"prop3", true}
            });
            writer.Write("r1");
            writer.Write("n1");
            writer.Write("n2");


            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<Relationship>().Which.Id.Should().Be(1L);
            value.Should().BeOfType<Relationship>().Which.StartNodeId.Should().Be(2L);  
            value.Should().BeOfType<Relationship>().Which.EndNodeId.Should().Be(3L);
            value.Should().BeOfType<Relationship>().Which.Type.Should().Be("RELATES_TO");
            value.Should().BeOfType<Relationship>().Which.Properties.Should()
                .HaveCount(1).And.Contain(new[]
            {
                new KeyValuePair<string, object>("prop3", true),
            });
            value.Should().BeOfType<Relationship>().Which.ElementId.Should().Be("r1");
            value.Should().BeOfType<Relationship>().Which.StartNodeElementId.Should().Be("n1");
            value.Should().BeOfType<Relationship>().Which.EndNodeElementId.Should().Be("n2");
        }
    }
}