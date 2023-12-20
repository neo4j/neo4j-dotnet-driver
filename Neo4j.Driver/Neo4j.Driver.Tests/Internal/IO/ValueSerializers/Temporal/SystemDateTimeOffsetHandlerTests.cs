// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;

public class SystemDateTimeOffsetHandlerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new SystemDateTimeOffsetSerializer();

    internal override IEnumerable<IPackStreamSerializer> SerializersNeeded => new IPackStreamSerializer[]
    {
        new ZonedDateTimeSerializer()
    };

    [Fact]
    public void ShouldSerializeDateTimeOffset()
    {
        var dateTime = new DateTimeOffset(1978, 12, 16, 12, 35, 59, 999, TimeSpan.FromSeconds(3060));
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(dateTime);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(3);
        reader.ReadStructSignature().Should().Be((byte)'F');
        reader.Read().Should().Be(282659759L);
        reader.Read().Should().Be(999000000L);
        reader.Read().Should().Be(3060L);
    }
}
