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
using FluentAssertions.Primitives;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.StructHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.StructHandlers
{
    public class DurationHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new DurationHandler();

        [Fact]
        public void ShouldWriteDuration()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(new Duration(10, 4, 300, 120));

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(4);
            reader.ReadStructSignature().Should().Be((byte) 'E');
            reader.Read().Should().Be(10L);
            reader.Read().Should().Be(4L);
            reader.Read().Should().Be(300L);
            reader.ReadInteger().Should().Be(120);
        }
        
        [Fact]
        public void ShouldReadDuration()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(DurationHandler.StructSize, DurationHandler.StructType);
            writer.Write(21L);
            writer.Write(8L);
            writer.Write(564L);
            writer.Write(865);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<Duration>().Which.Months.Should().Be(21L);
            value.Should().BeOfType<Duration>().Which.Days.Should().Be(8L);
            value.Should().BeOfType<Duration>().Which.Seconds.Should().Be(564L);
            value.Should().BeOfType<Duration>().Which.Nanos.Should().Be(865);
        }
        
    }
}