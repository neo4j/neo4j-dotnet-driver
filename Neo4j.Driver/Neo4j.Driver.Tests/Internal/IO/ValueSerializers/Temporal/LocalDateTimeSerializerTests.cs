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

using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers.Temporal;

public class LocalDateTimeSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new LocalDateTimeSerializer();

    [Fact]
    public void ShouldSerializeDateTime()
    {
        var dateTime = new LocalDateTime(1978, 12, 16, 12, 35, 59, 128000987);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(dateTime);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(2);
        reader.ReadStructSignature().Should().Be((byte)'d');
        reader.Read().Should().Be(282659759L);
        reader.Read().Should().Be(128000987L);
    }

    [Fact]
    public void ShouldDeserializeDateTime()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(LocalDateTimeSerializer.StructSize, LocalDateTimeSerializer.StructType);
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

    [Fact]
    public void ShouldDeserializeSpanDateTime()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(LocalDateTimeSerializer.StructSize, LocalDateTimeSerializer.StructType);
        writer.Write(282659759);
        writer.Write(128000987);

        var reader = CreateSpanReader(writerMachine.GetOutput());
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
