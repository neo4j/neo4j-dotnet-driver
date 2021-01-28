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

using System;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal
{
    public class OffsetTimeSerializerTests : PackStreamSerializerTests
    {
        internal override IPackStreamSerializer SerializerUnderTest => new OffsetTimeSerializer();

        [Fact]
        public void ShouldSerializeTimeWithOffset()
        {
            var time = new OffsetTime(12, 35, 59, 128000987, (int)TimeSpan.FromMinutes(150).TotalSeconds);
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(time);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(2);
            reader.ReadStructSignature().Should().Be((byte) 'T');
            reader.Read().Should().Be(45359128000987L);
            reader.Read().Should().Be((long)time.OffsetSeconds);
        }
        
        [Fact]
        public void ShouldDeserializeTimeWithOffset()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(OffsetTimeSerializer.StructSize, OffsetTimeSerializer.StructType);
            writer.Write(45359128000987);
            writer.Write((int)TimeSpan.FromMinutes(150).TotalSeconds);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull();
            value.Should().BeOfType<OffsetTime>().Which.Hour.Should().Be(12);
            value.Should().BeOfType<OffsetTime>().Which.Minute.Should().Be(35);
            value.Should().BeOfType<OffsetTime>().Which.Second.Should().Be(59);
            value.Should().BeOfType<OffsetTime>().Which.Nanosecond.Should().Be(128000987);
            value.Should().BeOfType<OffsetTime>().Which.OffsetSeconds.Should().Be((int)TimeSpan.FromMinutes(150).TotalSeconds);
        }
        
    }
}