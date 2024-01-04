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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers.Temporal;

public class SystemTimeSpanSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new SystemTimeSpanSerializer();

    internal override IEnumerable<IPackStreamSerializer> SerializersNeeded => new IPackStreamSerializer[]
    {
        new LocalTimeSerializer()
    };

    [Fact]
    public void ShouldSerializeTime()
    {
        var time = new TimeSpan(0, 12, 35, 59, 999);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(time);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be((byte)'t');
        reader.Read().Should().Be(45359999000000L);
    }

    [Fact]
    public void ShouldNotWriteNegativeTime()
    {
        var time = new TimeSpan(0, 0, 0, 0, -999);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var ex = Record.Exception(() => writer.Write(time));

        ex.Should().NotBeNull().And.BeOfType<ProtocolException>();
    }

    [Fact]
    public void ShouldNotWriteTimeLargerThanDay()
    {
        var time = new TimeSpan(0, 24, 0, 0, 0);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        var ex = Record.Exception(() => writer.Write(time));

        ex.Should().NotBeNull().And.BeOfType<ProtocolException>();
    }
}
