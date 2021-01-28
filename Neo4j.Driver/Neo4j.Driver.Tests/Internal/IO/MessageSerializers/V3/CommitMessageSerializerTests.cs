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

using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V3
{
    public class CommitMessageSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new CommitMessageSerializer();

        [Fact]
        public void ShouldThrowOnDeserialize()
        {
            var handler = SerializerUnderTest;

            var ex = Record.Exception(() =>
                handler.Deserialize(Mock.Of<IPackStreamReader>(), BoltProtocolV3MessageFormat.MsgCommit, 0));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldSerialize()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(CommitMessage.Commit);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(0);
            reader.ReadStructSignature().Should().Be(BoltProtocolV3MessageFormat.MsgCommit);
        }
    }
}