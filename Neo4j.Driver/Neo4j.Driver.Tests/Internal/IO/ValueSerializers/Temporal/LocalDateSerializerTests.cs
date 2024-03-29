﻿// Copyright (c) "Neo4j"
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
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.ValueSerializers.Temporal;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers.Temporal;

public class LocalDateSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new LocalDateSerializer();

    [Fact]
    public void ShouldSerializeDate()
    {
        var date = new LocalDate(1950, 8, 31);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(date);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be((byte)'D');
        reader.Read().Should().Be(-7063L);
    }

    [Fact]
    public void ShouldDeserializeDate()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

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

#if NET6_0_OR_GREATER
    [Fact]
    public void ShouldSerializeDateOnly()
    {
        var date = new DateOnly(1950, 8, 31);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(date);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.ReadStructSignature().Should().Be((byte)'D');
        reader.Read().Should().Be(-7063L);
    }
#endif

    [Fact]
    public void ShouldDeserializeSpanDate()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(LocalDateSerializer.StructSize, LocalDateSerializer.StructType);
        writer.Write(-7063L);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        value.Should().NotBeNull();
        value.Should().BeOfType<LocalDate>().Which.Year.Should().Be(1950);
        value.Should().BeOfType<LocalDate>().Which.Month.Should().Be(8);
        value.Should().BeOfType<LocalDate>().Which.Day.Should().Be(31);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void ShouldSerializeSpanDateOnly()
    {
        var date = new DateOnly(1950, 8, 31);
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(date);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(1);
        reader.NextByte().Should().Be((byte)'D');
        reader.Read().Should().Be(-7063L);
    }
#endif
}
