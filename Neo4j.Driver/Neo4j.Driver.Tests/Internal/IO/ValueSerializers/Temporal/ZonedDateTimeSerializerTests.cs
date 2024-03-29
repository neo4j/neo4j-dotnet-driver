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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests.Internal.IO.Utils;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete - but we still test obsolete members

namespace Neo4j.Driver.Tests.Internal.IO.ValueSerializers.Temporal;

public class ZonedDateTimeSerializerTests : PackStreamSerializerTests
{
    internal override IPackStreamSerializer SerializerUnderTest => new ZonedDateTimeSerializer();

    private new PackStreamWriterMachine CreateWriterMachine()
    {
        return CreateWriterMachine(BoltProtocolVersion.V4_0);
    }

    [Fact]
    public void ShouldSerializeDateTimeWithOffset()
    {
        var dateTime = new ZonedDateTime(
            1978,
            12,
            16,
            12,
            35,
            59,
            128000987,
            Zone.Of((int)TimeSpan.FromMinutes(-150).TotalSeconds));

        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(dateTime);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
        reader.ReadStructHeader().Should().Be(3);
        reader.ReadStructSignature().Should().Be((byte)'F');
        reader.Read().Should().Be(282659759L);
        reader.Read().Should().Be(128000987L);
        reader.Read().Should().Be(-9000L);
    }

    [Fact]
    public void ShouldDeserializeDateTimeWithOffset()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithOffset);
        writer.Write(282659759);
        writer.Write(128000987);
        writer.Write(-9000);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        ValidateOffset(value);
    }

    [Fact]
    public void ShouldSerializeDateTimeWithZoneId()
    {
        var dateTime = new ZonedDateTime(1978, 12, 16, 12, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.Write(dateTime);

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Struct);
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
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithId);
        writer.Write(282659759);
        writer.Write(128000987);
        writer.Write("Europe/Istanbul");

        var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
        var reader = readerMachine.Reader();
        var value = reader.Read();

        ValidateZoneId(value);
    }

    [Fact]
    public void ShouldDeserializeSpanDateTimeWithOffset()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithOffset);
        writer.Write(282659759);
        writer.Write(128000987);
        writer.Write(-9000);

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        ValidateOffset(value);
    }

    private static void ValidateOffset(object value)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<ZonedDateTime>().Which.Year.Should().Be(1978);
        value.Should().BeOfType<ZonedDateTime>().Which.Month.Should().Be(12);
        value.Should().BeOfType<ZonedDateTime>().Which.Day.Should().Be(16);
        value.Should().BeOfType<ZonedDateTime>().Which.Hour.Should().Be(12);
        value.Should().BeOfType<ZonedDateTime>().Which.Minute.Should().Be(35);
        value.Should().BeOfType<ZonedDateTime>().Which.Second.Should().Be(59);
        value.Should().BeOfType<ZonedDateTime>().Which.Nanosecond.Should().Be(128000987);
        value.Should()
            .BeOfType<ZonedDateTime>()
            .Which.Zone.Should()
            .BeOfType<ZoneOffset>()
            .Which.OffsetSeconds.Should()
            .Be((int)TimeSpan.FromMinutes(-150).TotalSeconds);
    }

    [Fact]
    public void ShouldDeserializeSpanDateTimeWithZoneId()
    {
        var writerMachine = CreateWriterMachine();
        var writer = writerMachine.Writer;

        writer.WriteStructHeader(ZonedDateTimeSerializer.StructSize, ZonedDateTimeSerializer.StructTypeWithId);
        writer.Write(282659759);
        writer.Write(128000987);
        writer.Write("Europe/Istanbul");

        var reader = CreateSpanReader(writerMachine.GetOutput());
        var value = reader.Read();

        ValidateZoneId(value);
    }

    private static void ValidateZoneId(object value)
    {
        value.Should().NotBeNull();
        value.Should().BeOfType<ZonedDateTime>().Which.Year.Should().Be(1978);
        value.Should().BeOfType<ZonedDateTime>().Which.Month.Should().Be(12);
        value.Should().BeOfType<ZonedDateTime>().Which.Day.Should().Be(16);
        value.Should().BeOfType<ZonedDateTime>().Which.Hour.Should().Be(12);
        value.Should().BeOfType<ZonedDateTime>().Which.Minute.Should().Be(35);
        value.Should().BeOfType<ZonedDateTime>().Which.Second.Should().Be(59);
        value.Should().BeOfType<ZonedDateTime>().Which.Nanosecond.Should().Be(128000987);
        value.Should()
            .BeOfType<ZonedDateTime>()
            .Which.Zone.Should()
            .BeOfType<ZoneId>()
            .Which.Id.Should()
            .Be("Europe/Istanbul");
    }
}
