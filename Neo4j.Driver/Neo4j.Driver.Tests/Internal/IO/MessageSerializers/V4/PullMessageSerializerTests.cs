// Copyright (c) 2002-2019 "Neo4j,"
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

using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Messaging.V4;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4
{
    public class PullMessageSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new PullMessageSerializer();

        [Fact]
        public void ShouldThrowOnDeserialize()
        {
            var handler = SerializerUnderTest;

            var ex = Record.Exception(() =>
                handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV4MessageFormat.MsgPullN, 0));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldSerializeIdAndN()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new PullMessage(2, 5));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(1);
            reader.ReadStructSignature().Should().Be(BoltProtocolV4MessageFormat.MsgPullN);
            reader.ReadMap().Should().HaveCount(2).And.Contain("n", 5L).And.Contain("qid", 2L);
        }

        [Fact]
        public void ShouldSerializeN()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new PullMessage(5));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(1);
            reader.ReadStructSignature().Should().Be(BoltProtocolV4MessageFormat.MsgPullN);
            reader.ReadMap().Should().HaveCount(1).And.Contain("n", 5L);
        }
    }
}