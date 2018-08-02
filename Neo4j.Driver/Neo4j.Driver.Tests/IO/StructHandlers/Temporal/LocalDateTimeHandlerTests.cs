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
using Neo4j.Driver.Internal.IO.ValueHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.StructHandlers
{
    public class LocalDateTimeHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new LocalDateTimeHandler();

        [Fact]
        public void ShouldWriteDateTime()
        {
            var dateTime = new LocalDateTime(1978, 12, 16, 12, 35, 59, 128000987);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be((byte) 'd');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(128000987L);
        }
        
        [Fact]
        public void ShouldReadDateTime()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(LocalDateTimeHandler.StructSize, LocalDateTimeHandler.StructType);
            writer.Write(282659759);
            writer.Write(128000987);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<LocalDateTime>().Which.Year.Should().Be(1978);
            value.Should().BeOfType<LocalDateTime>().Which.Month.Should().Be(12);
            value.Should().BeOfType<LocalDateTime>().Which.Day.Should().Be(16);
            value.Should().BeOfType<LocalDateTime>().Which.Hour.Should().Be(12);
            value.Should().BeOfType<LocalDateTime>().Which.Minute.Should().Be(35);
            value.Should().BeOfType<LocalDateTime>().Which.Second.Should().Be(59);
            value.Should().BeOfType<LocalDateTime>().Which.Nanosecond.Should().Be(128000987);
        }
        
    }
}