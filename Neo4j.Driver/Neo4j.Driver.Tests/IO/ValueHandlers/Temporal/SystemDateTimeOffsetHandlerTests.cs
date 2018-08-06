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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueHandlers;
using Neo4j.Driver.Tests.IO.MessageHandlers;
using Xunit;

namespace Neo4j.Driver.Tests.IO.ValueHandlers
{
    public class SystemDateTimeOffsetHandlerTests : StructHandlerTests
    {
        internal override IPackStreamStructHandler HandlerUnderTest => new SystemDateTimeOffsetHandler();

        internal override IEnumerable<IPackStreamStructHandler> HandlersNeeded => new IPackStreamStructHandler[]
        {
            new ZonedDateTimeHandler()
        };
        
        [Fact]
        public void ShouldWriteDateTimeOffset()
        {
            var dateTime = new DateTimeOffset(1978, 12, 16, 12, 35, 59, 999, TimeSpan.FromSeconds(3060));
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
            reader.Read().Should().Be(3060L);
        }

    }
}