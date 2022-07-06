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
    public class ElementNodeSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new ElementNodeSerializer();
        
        [Fact]
        public void ShouldDeserialize()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(3, ElementNodeSerializer.Node);
            writer.Write(1);
            writer.Write(new List<string> { "Label1", "Label2" });
            writer.Write(new Dictionary<string, object>
            {
                {"prop1", "something"},
                {"prop2", 15},
                {"prop3", true}
            });
            writer.Write("1");

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<Node>().Which.Id.Should().Be(1L);
            value.Should().BeOfType<Node>().Which.Labels.Should().Equal(new[] { "Label1", "Label2" });
            value.Should().BeOfType<Node>().Which.Properties.Should().HaveCount(3).And.Contain(new[]
            {
                new KeyValuePair<string, object>("prop1", "something"),
                new KeyValuePair<string, object>("prop2", 15L),
                new KeyValuePair<string, object>("prop3", true),
            });
            value.Should().BeOfType<Node>().Which.ElementId.Should().Be("1");
        }
    }
}