// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Internal.IO.ValueSerializers.Temporal
{
    public class UtcZonedDateTimeSerializerTests : PackStreamSerializerTests
    {
        public static object[][] IdPairs = new[]
        {
            new object[]
            {
                new ZonedDateTime(2022, 6, 14, 15, 21, 18, 183_000_000, Zone.Of("Europe/Berlin")),
                (seconds: 1655212878L, nanos: 183_000_000L, zoneId: "Europe/Berlin")
            },
            new object[]
            {
                new ZonedDateTime(2022, 6, 14, 22, 6, 18, 183_000_000, Zone.Of("Australia/Eucla")),
                (seconds: 1655212878L, nanos: 183_000_000L, zoneId: "Australia/Eucla")
            },
            new object[]
            {
                new ZonedDateTime(2020, 6, 15, 4, 30, 0, 183_000_000, Zone.Of("Pacific/Honolulu")),
                (seconds: 1592231400L, nanos: 183_000_000L, zoneId: "Pacific/Honolulu")
            }
        };
        public static object[][] OffsetPairs = new[]
        {
            new object[]
            {
                new ZonedDateTime(1978, 12, 16, 10, 05, 59, 128000987, new ZoneOffset(-150 * 60)),
                (seconds: 282659759L, nanos: 128000987L, offset: new ZoneOffset(-150 * 60))
            },
            new object[]
            {
                new ZonedDateTime(2022, 6, 14, 15, 21, 18, 183_000_000, new ZoneOffset(120 * 60)),
                (seconds: 1655212878L, nanos: 183_000_000L, offset: new ZoneOffset(120 * 60))
            },
            new object[]
            {
                new ZonedDateTime(2020, 6, 15, 12, 30, 0, 42, new ZoneOffset(-2 * 60 * 60)),
                (seconds: 1592231400L, nanos: 42L, offset: new ZoneOffset(-2 * 60 * 60))
            }
        };

        internal override IPackStreamSerializer SerializerUnderTest => new UtcZonedDateTimeSerializer();

        [Theory, MemberData(nameof(OffsetPairs))]
        public void ShouldSerializeDateTimeWithOffset(ZonedDateTime inDate, (long seconds, long nanos, ZoneOffset offset) expected)
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.Write(inDate);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte) 'I');
            writer.Write(expected.seconds);
            writer.Write(expected.nanos);
            writer.Write(expected.offset.OffsetSeconds);
        }

        [Theory, MemberData(nameof(OffsetPairs))]
        public void ShouldDeserializeDateTimeWithOffset(ZonedDateTime expected, (long seconds, long nanos, ZoneOffset offset) inDate)
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(UtcZonedDateTimeSerializer.StructSize, UtcZonedDateTimeSerializer.StructTypeWithOffset);
            writer.Write(inDate.seconds);
            writer.Write(inDate.nanos);
            writer.Write(inDate.offset.OffsetSeconds);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            var dateTime = value.Should().BeOfType<ZonedDateTime>();
            dateTime.Which.Should().Be(expected);

            dateTime.Which.Zone.Should().BeOfType<ZoneOffset>()
                .Which.Should().Be(inDate.offset);
        }

        [WindowsFact]
        public void ShouldSerializeDateTimeWithZoneId_Windows_Istanbul()
        {
            var inDate = new ZonedDateTime(1978, 12, 16, 12, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
            var expected = (seconds: 282652559L, nanos: 128000987L, zoneId: "Europe/Istanbul");
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();
            writer.Write(inDate);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'i');
            reader.Read().Should().Be(expected.seconds);
            reader.Read().Should().Be(expected.nanos);
            reader.Read().Should().Be(expected.zoneId);
        }

        [WindowsFact]
        public void ShouldDeserializeDateTimeWithZoneId_Windows_Istanbul()
        {
            var expected = new ZonedDateTime(1978, 12, 16, 12, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
            var inDate = (seconds: 282652559L, nanos: 128000987L, zoneId: "Europe/Istanbul");
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(UtcZonedDateTimeSerializer.StructSize, UtcZonedDateTimeSerializer.StructTypeWithId);
            writer.Write(inDate.seconds);
            writer.Write(inDate.nanos);
            writer.Write(inDate.zoneId);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull().And.Be(expected);
        }

        [UnixFact]
        public void ShouldSerializeDateTimeWithZoneId_Unix_Istanbul()
        {
            var inDate = new ZonedDateTime(1978, 12, 16, 13, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
            var expected = (seconds: 282652559L, nanos: 128000987L, zoneId: "Europe/Istanbul");
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();
            writer.Write(inDate);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'i');
            reader.Read().Should().Be(expected.seconds);
            reader.Read().Should().Be(expected.nanos);
            reader.Read().Should().Be(expected.zoneId);
        }

        [UnixFact]
        public void ShouldDeserializeDateTimeWithZoneId_Unix_Istanbul()
        {
            var expected = new ZonedDateTime(1978, 12, 16, 13, 35, 59, 128000987, Zone.Of("Europe/Istanbul"));
            var inDate = (seconds: 282652559L, nanos: 128000987L, zoneId: "Europe/Istanbul");
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(UtcZonedDateTimeSerializer.StructSize, UtcZonedDateTimeSerializer.StructTypeWithId);
            writer.Write(inDate.seconds);
            writer.Write(inDate.nanos);
            writer.Write(inDate.zoneId);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull().And.Be(expected);
        }

        [Theory, MemberData(nameof(IdPairs))]
        public void ShouldSerializeDateTimeWithZoneId(ZonedDateTime inDate, (long seconds, long nanos, string zoneId) expected)
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();
            writer.Write(inDate);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'i');
            reader.Read().Should().Be(expected.seconds);
            reader.Read().Should().Be(expected.nanos);
            reader.Read().Should().Be(expected.zoneId);
        }

        [Theory, MemberData(nameof(IdPairs))]
        public void ShouldDeserializeDateTimeWithZoneId(ZonedDateTime expected, (long seconds, long nanos, string zoneId) inDate)
        {
            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();

            writer.WriteStructHeader(UtcZonedDateTimeSerializer.StructSize, UtcZonedDateTimeSerializer.StructTypeWithId);
            writer.Write(inDate.seconds);
            writer.Write(inDate.nanos);
            writer.Write(inDate.zoneId);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var value = reader.Read();

            value.Should().NotBeNull().And.Be(expected);
        }

        [Fact]
        public void ShouldSerializeUtcAndNonUtcSecondsToSameValue()
        {
            var bstValue = DateTimeOffset.Parse("2022-06-16T11:12:06.3668289+01:00");
            var zoned = new ZonedDateTime(bstValue);
            var utc = DateTimeOffset.Parse("2022-06-16T10:12:06.3668289+00:00");
            var zonedUtc = new ZonedDateTime(utc);

            var writerMachine = CreateWriterMachine();
            var writer = writerMachine.Writer();
            writer.Write(zoned);
            writer.Write(zonedUtc);

            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();


            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'I');
            var bstSecs = reader.Read();
            reader.Read();
            reader.Read().Should().Be(3600);

            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte)'I');
            var utcSecs = reader.Read();
            reader.Read();
            reader.Read().Should().Be(0);

            bstSecs.Should().Equals(utcSecs);
        }
    }
}