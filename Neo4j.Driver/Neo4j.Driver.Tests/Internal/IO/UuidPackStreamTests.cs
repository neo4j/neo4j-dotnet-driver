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
        var guid = Guid.NewGuid();;
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
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var machine = CreateWriterMachine();

        machine.Writer.WriteUuid(guid);

        var bytes = machine.GetOutput();
        var expectedUuidBytes = new byte[]
            { 0x55, 0x0e, 0x84, 0x00, 0xe2, 0x9b, 0x41, 0xd4, 0xa7, 0x16, 0x44, 0x66, 0x55, 0x44, 0x00, 0x00 };

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
        var guid = Guid.NewGuid();;
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
        // to catch byte-order swaps
        var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var machine = CreateWriterMachine();
        machine.Writer.WriteUuid(guid);

        var reader = CreateReaderMachine(machine.GetOutput()).Reader();

        reader.Read().Should().Be(guid);
    }

    [Fact]
    public void Read_FromManuallyConstructedBytes_ReturnsExpectedGuid()
    {
        var guid = Guid.NewGuid();;
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
        var guid = Guid.NewGuid();;
        var uuidBytes = guid.ToByteArray(bigEndian: true);

        var wireBytes = new byte[17];
        wireBytes[0] = PackStream.Uuid;
        uuidBytes.CopyTo(wireBytes, 1);

        var reader = CreateReaderMachine(wireBytes).Reader();

        reader.ReadUuid().Should().Be(guid);
    }
}
