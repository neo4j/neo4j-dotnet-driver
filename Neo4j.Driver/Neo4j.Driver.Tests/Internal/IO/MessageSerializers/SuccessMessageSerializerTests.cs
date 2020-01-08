// Copyright (c) 2002-2020 "Neo4j,"
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

using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers
{
    public class SuccessMessageSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new SuccessMessageSerializer();

        [Fact]
        public void ShouldThrowOnSerialize()
        {
            var handler = SerializerUnderTest;

            var ex = Record.Exception(() =>
                handler.Serialize(Mock.Of<IPackStreamWriter>(),
                    new SuccessMessage(new Dictionary<string, object> {{"fields", 1}})));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldDeserialize()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(1, BoltProtocolV3MessageFormat.MsgSuccess);
            writer.WriteMapHeader(2);
            writer.Write("fields");
            writer.Write(1L);
            writer.Write("statistics");
            writer.Write(true);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<SuccessMessage>().Which.Meta.Should()
                .HaveCount(2).And
                .Contain(new[]
                {
                    new KeyValuePair<string, object>("fields", 1L), new KeyValuePair<string, object>("statistics", true)
                });
        }

    }
}