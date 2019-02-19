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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal
{
    public class ZonedDateTimeSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new ZonedDateTimeSerializer();

        [Fact]
        public void ShouldSerializeDateTimeWithOffset()
        {
            var dateTime = new ZonedDateTime(1978, 12, 16, 12, 35, 59, 128000987,
                Zone.Of((int) TimeSpan.FromMinutes(-150).TotalSeconds));
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte) 'F');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(128000987L);
            reader.Read().Should().Be(-9000L);
        }
        
        [Fact]
        public void ShouldDeserializeDateTimeWithOffset()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithOffset);
            writer.Write(282659759);
            writer.Write(128000987);
            writer.Write(-9000);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<ZonedDateTime>().Which.Year.Should().Be(1978);
            value.Should().BeOfType<ZonedDateTime>().Which.Month.Should().Be(12);
            value.Should().BeOfType<ZonedDateTime>().Which.Day.Should().Be(16);
            value.Should().BeOfType<ZonedDateTime>().Which.Hour.Should().Be(12);
            value.Should().BeOfType<ZonedDateTime>().Which.Minute.Should().Be(35);
            value.Should().BeOfType<ZonedDateTime>().Which.Second.Should().Be(59);
            value.Should().BeOfType<ZonedDateTime>().Which.Nanosecond.Should().Be(128000987);
            value.Should().BeOfType<ZonedDateTime>().Which.Zone.Should().BeOfType<ZoneOffset>().Which.OffsetSeconds.Should().Be((int)TimeSpan.FromMinutes(-150).TotalSeconds);
        }

        [Fact]
        public void ShouldSerializeDateTimeWithZoneId()
        {
            var dateTime = new ZonedDateTime(1978, 12, 16, 12, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'f');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(128000987L);
            reader.Read().Should().Be("Europe/Istanbul");
        }

        [Fact]
        public void ShouldDeserializeDateTimeWithZoneId()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithId);
            writer.Write(282659759);
            writer.Write(128000987);
            writer.Write("Europe/Istanbul");

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<ZonedDateTime>().Which.Year.Should().Be(1978);
            value.Should().BeOfType<ZonedDateTime>().Which.Month.Should().Be(12);
            value.Should().BeOfType<ZonedDateTime>().Which.Day.Should().Be(16);
            value.Should().BeOfType<ZonedDateTime>().Which.Hour.Should().Be(12);
            value.Should().BeOfType<ZonedDateTime>().Which.Minute.Should().Be(35);
            value.Should().BeOfType<ZonedDateTime>().Which.Second.Should().Be(59);
            value.Should().BeOfType<ZonedDateTime>().Which.Nanosecond.Should().Be(128000987);
            value.Should().BeOfType<ZonedDateTime>().Which.Zone.Should().BeOfType<ZoneId>().Which.Id.Should().Be("Europe/Istanbul");
        }
    }
}