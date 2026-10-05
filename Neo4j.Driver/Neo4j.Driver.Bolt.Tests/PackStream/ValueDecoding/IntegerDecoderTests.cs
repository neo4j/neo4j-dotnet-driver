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

using System.Buffers;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class IntegerDecoderTests : DecoderTestsBase<IntegerDecoder>
{
    [Test]
    public void HandledMarkerBytesIncludesInt8Int16Int32Int64()
    {
        var expected = new[] { PackStreamMarker.Int8, PackStreamMarker.Int16, PackStreamMarker.Int32, PackStreamMarker.Int64 };
        Subject.HandledMarkerBytes.Should().BeEquivalentTo(expected);
    }

    #region INT_8 (marker 0xC8 + 1 byte signed)

    [Test]
    public void DecodesInt8Positive42()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8, 0x2A]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(42);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesInt8MaxPositive127()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8, 0x7F]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(127);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesInt8MinNegativeMinus128()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8, 0x80]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-128);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesInt8Minus17()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8, 0xEF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-17);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesInt8MinusOne()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-1);
        result.BytesConsumed.Should().Be(2);
    }

    #endregion

    #region INT_16 (marker 0xC9 + 2 bytes big-endian signed)

    [Test]
    public void DecodesInt16Positive256()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0x01, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(256);
        result.BytesConsumed.Should().Be(3);
    }

    [Test]
    public void DecodesInt16MaxPositive32767()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0x7F, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(32767);
        result.BytesConsumed.Should().Be(3);
    }

    [Test]
    public void DecodesInt16MinNegativeMinus32768()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0x80, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-32768);
        result.BytesConsumed.Should().Be(3);
    }

    [Test]
    public void DecodesInt16MinusOne()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0xFF, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-1);
        result.BytesConsumed.Should().Be(3);
    }

    [Test]
    public void DecodesInt16Zero()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(0);
        result.BytesConsumed.Should().Be(3);
    }

    #endregion

    #region INT_32 (marker 0xCA + 4 bytes big-endian signed)

    [Test]
    public void DecodesInt32Positive65536()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0x00, 0x01, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(65536);
        result.BytesConsumed.Should().Be(5);
    }

    [Test]
    public void DecodesInt32MaxPositive2147483647()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0x7F, 0xFF, 0xFF, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(2147483647);
        result.BytesConsumed.Should().Be(5);
    }

    [Test]
    public void DecodesInt32MinNegativeMinus2147483648()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0x80, 0x00, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-2147483648);
        result.BytesConsumed.Should().Be(5);
    }

    [Test]
    public void DecodesInt32MinusOne()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0xFF, 0xFF, 0xFF, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-1);
        result.BytesConsumed.Should().Be(5);
    }

    [Test]
    public void DecodesInt32Zero()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0x00, 0x00, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(0);
        result.BytesConsumed.Should().Be(5);
    }

    #endregion

    #region INT_64 (marker 0xCB + 8 bytes big-endian signed)

    [Test]
    public void DecodesInt64Positive4294967296()
    {
        // 2^32
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(4294967296L);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesInt64MaxValue()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(long.MaxValue);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesInt64MinValue()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(long.MinValue);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesInt64MinusOne()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(-1L);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesInt64Zero()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        var result = Subject.Decode(buffer);
        result.Value.IntValue.Should().Be(0L);
        result.BytesConsumed.Should().Be(9);
    }

    #endregion

    #region Error cases

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        var buffer = ReadOnlySequence<byte>.Empty;
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Null]);
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ThrowsOnInt8BufferTooShort()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int8]);
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnInt16BufferTooShort()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int16, 0x00]);
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnInt32BufferTooShort()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Int32, 0x00, 0x00, 0x00]);
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnInt64BufferTooShort()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Int64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        Action act = () => Subject.Decode(buffer);
        act.Should().Throw<ProtocolException>();
    }

    #endregion
}
