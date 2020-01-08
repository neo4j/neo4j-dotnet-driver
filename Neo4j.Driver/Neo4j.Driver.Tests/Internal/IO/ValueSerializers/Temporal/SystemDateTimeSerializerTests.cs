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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal
{
    public class SystemDateTimeSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new SystemDateTimeSerializer();

        internal override IEnumerable<IPackStreamSerializer> SerializersNeeded => new IPackStreamSerializer[]
        {
            new LocalDateTimeSerializer(), new ZonedDateTimeSerializer()
        };

        [Fact]
        public void ShouldSerializeDateTimeLocal()
        {
            var dateTime = new DateTime(1978, 12, 16, 12, 35, 59, 999, DateTimeKind.Local);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be((byte) 'd');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(999000000L);
        }

        [Fact]
        public void ShouldSerializeDateTimeUnspecified()
        {
            var dateTime = new DateTime(1978, 12, 16, 12, 35, 59, 999, DateTimeKind.Unspecified);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be((byte)'d');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(999000000L);
        }

        [Fact]
        public void ShouldSerializeDateTimeUtc()
        {
            var dateTime = new DateTime(1978, 12, 16, 12, 35, 59, 999, DateTimeKind.Utc);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(dateTime);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'F');
            reader.Read().Should().Be(282659759L);
            reader.Read().Should().Be(999000000L);
            reader.Read().Should().Be(0L);
        }

    }
}