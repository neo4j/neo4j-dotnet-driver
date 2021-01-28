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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Tests.IO.MessageHandlers;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV1MessageFormat;

namespace Neo4j.Driver.Tests.IO.ValueHandlers
{
    public class ResetMessageHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new ResetMessageHandler();

        [Fact]
        public void ShouldThrowOnRead()
        {
            var handler = HandlerUnderTest;

            var ex = Record.Exception(() =>
                handler.Read(Mock.Of<IPackStreamReader>(), MsgReset, 0));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldWrite()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(ResetMessage.Reset);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(0);
            reader.ReadStructSignature().Should().Be(MsgReset);
        }
    }
}