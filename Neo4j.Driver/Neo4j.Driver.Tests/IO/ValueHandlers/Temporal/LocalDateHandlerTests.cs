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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueHandlers;
using Neo4j.Driver.Tests.IO.MessageHandlers;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests.IO.ValueHandlers
{
    public class LocalDateHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new LocalDateHandler();

        [Fact]
        public void ShouldWriteDate()
        {
            var date = new LocalDate(1950, 8, 31);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(date);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(1);
            reader.ReadStructSignature().Should().Be((byte) 'D');
            reader.Read().Should().Be(-7063L);
        }
        
        [Fact]
        public void ShouldReadDate()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(LocalDateHandler.StructSize, LocalDateHandler.StructType);
            writer.Write(-7063L);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<LocalDate>().Which.Year.Should().Be(1950);
            value.Should().BeOfType<LocalDate>().Which.Month.Should().Be(8);
            value.Should().BeOfType<LocalDate>().Which.Day.Should().Be(31);
        }
        
    }
}