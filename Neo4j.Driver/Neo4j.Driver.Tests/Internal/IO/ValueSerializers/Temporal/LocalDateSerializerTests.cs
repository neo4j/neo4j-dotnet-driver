// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal
{
    public class LocalDateSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new LocalDateSerializer();

        [Fact]
        public void ShouldSerializeDate()
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
        public void ShouldDeserializeDate()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(LocalDateSerializer.StructSize, LocalDateSerializer.StructType);
            writer.Write(-7063L);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<LocalDate>().Which.Year.Should().Be(1950);
            value.Should().BeOfType<LocalDate>().Which.Month.Should().Be(8);
            value.Should().BeOfType<LocalDate>().Which.Day.Should().Be(31);
        }

        [Fact]
        [Conditional("NET6_0_OR_GREATER")]
        public void ShouldSerializeDateOnly()
        {
            var date = new DateOnly(1950, 8, 31);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(date);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(1);
            reader.ReadStructSignature().Should().Be((byte)'D');
            reader.Read().Should().Be(-7063L);
        }
    }
}