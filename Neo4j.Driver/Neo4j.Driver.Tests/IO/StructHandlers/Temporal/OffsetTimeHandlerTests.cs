// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Primitives;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.StructHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO.StructHandlers
{
    public class OffsetTimeHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new OffsetTimeHandler();

        [Fact]
        public void ShouldWriteTimeWithOffset()
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
        public void ShouldReadTimeWithOffset()
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(OffsetTimeHandler.StructSize, OffsetTimeHandler.StructType);
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