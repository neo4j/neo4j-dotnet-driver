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
using FluentAssertions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests.Internal.IO.Utils;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO;

public class UuidPackStreamTests
{
    private static readonly BoltProtocolVersion V6_1 = new(6, 1);

    private static PackStreamWriterMachine CreateWriterMachine()
    {
        var format = new MessageFormat(V6_1, TestDriverContext.MockContext);
        return new PackStreamWriterMachine(stream => new PackStreamWriter(format, stream));
    }

    private static PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
    {
        var format = new MessageFormat(V6_1, TestDriverContext.MockContext);
        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(format, stream, new ByteBuffers()));
    }

    [Fact]
    public void WriteUuid_ShouldStartWithMarkerByte()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();

        machine.Writer.WriteUuid(guid);

        var bytes = machine.GetOutput();
        bytes[0].Should().Be(PackStream.Uuid, "first byte must be the 0xE0 UUID marker");
    }

    [Fact]
    public void WriteUuid_ShouldWriteExactly17Bytes()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();

        machine.Writer.WriteUuid(guid);

        machine.GetOutput().Should().HaveCount(17, "1 marker byte + 16 UUID bytes");
    }

    [Fact]
    public void WriteUuid_ShouldWriteBigEndianBytes()
    {
        var guid = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var machine = CreateWriterMachine();

        machine.Writer.WriteUuid(guid);

        var bytes = machine.GetOutput();
        var expectedUuidBytes = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef
        };

        bytes[1..].Should().Equal(expectedUuidBytes, "UUID bytes must be in big-endian (RFC 4122) byte order");
    }

    [Fact]
    public void WriteUuid_TwoDistinctGuids_ProduceDifferentBytes()
    {
        var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var machine1 = CreateWriterMachine();
        machine1.Writer.WriteUuid(guid1);

        var machine2 = CreateWriterMachine();
        machine2.Writer.WriteUuid(guid2);

        machine1.GetOutput().Should().NotEqual(machine2.GetOutput());
    }

    [Fact]
    public void ReadUuid_ShouldPeekAsUuidType()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();

        reader.PeekNextType().Should().Be(PackStreamType.Uuid);
    }

    [Fact]
    public void ReadUuid_ShouldReturnGuidType()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();
        var result = reader.Read();

        result.Should().BeOfType<Guid>();
    }

    [Fact]
    public void ReadUuid_ShouldReturnCorrectGuid()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();

        reader.Read().Should().Be(guid);
    }

    [Fact]
    public void WriteUuid_ThenRead_RoundTrips()
    {
        var guid = Guid.NewGuid();
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();

        reader.Read().Should().Be(guid);
    }

    [Fact]
    public void WriteUuid_ThenRead_PreservesAllBits()
    {
        // Sequential bytes: any reordering is immediately visible in the wire representation.
        var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();

        reader.Read().Should().Be(guid);
    }

    [Fact]
    public void Read_FromManuallyConstructedBytes_ReturnsExpectedGuid()
    {
        var guid = Guid.NewGuid();
        var uuidBytes = guid.ToByteArray(bigEndian: true);

        // Build the wire bytes manually: marker + 16 UUID bytes
        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Uuid;
        uuidBytes.CopyTo(wireBytes, 1);

        var reader = CreateReaderMachine(wireBytes).Reader();

        reader.Read().Should().Be(guid);
    }

    [Fact]
    public void ReadUuid_DirectMethod_ReturnsExpectedGuid()
    {
        var guid = Guid.NewGuid();
        var uuidBytes = guid.ToByteArray(bigEndian: true);

        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Uuid;
        uuidBytes.CopyTo(wireBytes, 1);

        var reader = CreateReaderMachine(wireBytes).Reader();

        reader.ReadUuid().Should().Be(guid);
    }

    [Fact]
    public void ReadUuid_WithWrongMarkerByte_ThrowsProtocolException()
    {
        // 0xC1 is the Float64 marker — anything other than 0xE0 should fail.
        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Float64;

        var reader = CreateReaderMachine(wireBytes).Reader();

        var act = () => reader.ReadUuid();

        act.Should().Throw<ProtocolException>()
            .WithMessage("Expected a UUID, but got: 0xC1");
    }

    private static PackStreamWriterMachine CreateWriterMachineForVersion(BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        return new PackStreamWriterMachine(stream => new PackStreamWriter(format, stream));
    }

    private static PackStreamReaderMachine CreateReaderMachineForVersion(byte[] bytes, BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(format, stream, new ByteBuffers()));
    }

    [Theory]
    [InlineData(6, 0)]
    [InlineData(5, 8)]
    [InlineData(5, 0)]
    [InlineData(4, 4)]
    public void WriteUuid_ThrowsProtocolException_WhenVersionBelowV6_1(int major, int minor)
    {
        var machine = CreateWriterMachineForVersion(new BoltProtocolVersion(major, minor));

        var act = () => machine.Writer.WriteUuid(Guid.NewGuid());

        act.Should().Throw<ProtocolException>()
            .WithMessage("*UUID*6.1*");
    }

    [Theory]
    [InlineData(6, 0)]
    [InlineData(5, 8)]
    [InlineData(5, 0)]
    [InlineData(4, 4)]
    public void Write_Guid_ThrowsProtocolException_WhenVersionBelowV6_1(int major, int minor)
    {
        var machine = CreateWriterMachineForVersion(new BoltProtocolVersion(major, minor));

        var act = () => machine.Writer.Write((object)Guid.NewGuid());

        act.Should().Throw<ProtocolException>()
            .WithMessage("*UUID*6.1*");
    }

    [Theory]
    [InlineData(6, 0)]
    [InlineData(5, 8)]
    [InlineData(5, 0)]
    [InlineData(4, 4)]
    public void ReadUuid_ThrowsProtocolException_WhenVersionBelowV6_1(int major, int minor)
    {
        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Uuid;
        Guid.NewGuid().ToByteArray(bigEndian: true).CopyTo(wireBytes, 1);

        var reader = CreateReaderMachineForVersion(wireBytes, new BoltProtocolVersion(major, minor)).Reader();

        var act = () => reader.ReadUuid();

        act.Should().Throw<ProtocolException>()
            .WithMessage("*UUID*6.1*");
    }

    [Fact]
    public void WriteUuid_DoesNotThrow_AtV6_1()
    {
        var machine = CreateWriterMachineForVersion(BoltProtocolVersion.V6_1);
        var act = () => machine.Writer.WriteUuid(Guid.NewGuid());
        act.Should().NotThrow();
    }

    private static byte[] WriteUuidToBytes(Guid guid, BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        var machine = new PackStreamWriterMachine(stream => new PackStreamWriter(format, stream));
        machine.Writer.WriteUuid(guid);
        return machine.GetOutput();
    }

    [Fact]
    public void SpanReader_PeekNextType_ReturnsUuid()
    {
        var bytes = WriteUuidToBytes(Guid.NewGuid(), V6_1);
        var spanReader = new SpanPackStreamReader(
            new MessageFormat(V6_1, TestDriverContext.MockContext),
            bytes);

        spanReader.PeekNextType().Should().Be(PackStreamType.Uuid);
    }

    [Fact]
    public void SpanReader_Read_ReturnsCorrectGuid()
    {
        var guid = Guid.NewGuid();
        var bytes = WriteUuidToBytes(guid, V6_1);
        var spanReader = new SpanPackStreamReader(
            new MessageFormat(V6_1, TestDriverContext.MockContext),
            bytes);

        spanReader.Read().Should().Be(guid);
    }

    [Fact]
    public void SpanReader_Read_PreservesAllBits()
    {
        var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
        var bytes = WriteUuidToBytes(guid, V6_1);
        var spanReader = new SpanPackStreamReader(
            new MessageFormat(V6_1, TestDriverContext.MockContext),
            bytes);

        spanReader.Read().Should().Be(guid);
    }

    [Fact]
    public void SpanReader_Read_FromManuallyConstructedBytes_ReturnsExpectedGuid()
    {
        var guid = Guid.NewGuid();
        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Uuid;
        guid.ToByteArray(bigEndian: true).CopyTo(wireBytes, 1);

        var spanReader = new SpanPackStreamReader(
            new MessageFormat(V6_1, TestDriverContext.MockContext),
            wireBytes);

        spanReader.Read().Should().Be(guid);
    }

    [Theory]
    [InlineData(6, 0)]
    [InlineData(5, 8)]
    [InlineData(5, 0)]
    [InlineData(4, 4)]
    public void SpanReader_Read_ThrowsProtocolException_WhenVersionBelowV6_1(int major, int minor)
    {
        // Wire bytes prepared at 6.1, then read with an older version.
        var wireBytes = WriteUuidToBytes(Guid.NewGuid(), V6_1);
        var oldVersion = new BoltProtocolVersion(major, minor);

        var act = () =>
        {
            var spanReader = new SpanPackStreamReader(
                new MessageFormat(oldVersion, TestDriverContext.MockContext),
                wireBytes);
            spanReader.Read();
        };

        act.Should().Throw<ProtocolException>()
            .WithMessage("*UUID*6.1*");
    }
}
