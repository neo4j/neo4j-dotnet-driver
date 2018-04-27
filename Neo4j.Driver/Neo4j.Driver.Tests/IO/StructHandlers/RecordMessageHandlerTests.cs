// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.StructHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.StructHandlers
{
    public class RecordMessageHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new RecordMessageHandler();

        [Fact]
        public void ShouldThrowOnWrite()
        {
            var handler = HandlerUnderTest;

            var ex = Record.Exception(() =>
                handler.Write(Mock.Of<IPackStreamWriter>(),
                    new RecordMessage(new object[] {"val1", 2, true})));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldRead()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(1, PackStream.MsgRecord);
            writer.WriteListHeader(6);
            writer.WriteNull();
            writer.Write(true);
            writer.Write(1);
            writer.Write(1.2);
            writer.Write('A');
            writer.Write("value");

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var value = readerMachine.Reader().Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<RecordMessage>().Which.Fields.Should()
                .HaveCount(6).And
                .Contain(new object[]
                {
                    true,
                    1L,
                    1.2,
                    "A",
                    "value"
                });
        }

    }
}