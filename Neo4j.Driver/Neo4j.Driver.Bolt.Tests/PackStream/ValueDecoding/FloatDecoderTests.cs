using System.Buffers;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class FloatDecoderTests : DecoderTestsBase<FloatDecoder>
{
    [Test]
    public void HandlesFloat64MarkerByte()
    {
        Subject.HandledMarkerBytes.Should().BeEquivalentTo([PackStreamMarker.Float64]);
    }

    [Test]
    public void DecodesPositiveValue()
    {
        // 0x40 0x09 0x21 0xFB 0x54 0x44 0x2D 0x18 ~= pi
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Float64, 0x40, 0x09, 0x21, 0xFB, 0x54, 0x44, 0x2D, 0x18]);

        var result = Subject.Decode(buffer);

        result.Value.FloatValue.Should().BeApproximately(Math.PI, 1e-15);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesNegativeValue()
    {
        // 0xC0 0x09 0x21 0xFB 0x54 0x44 0x2D 0x18 ~= -pi
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Float64, 0xC0, 0x09, 0x21, 0xFB, 0x54, 0x44, 0x2D, 0x18]);

        var result = Subject.Decode(buffer);

        result.Value.FloatValue.Should().BeApproximately(-Math.PI, 1e-15);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesZero()
    {
        // 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 = 0.0
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Float64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        var result = Subject.Decode(buffer);

        result.Value.FloatValue.Should().Be(0.0);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void DecodesOne()
    {
        // 0x3F 0xF0 0x00 0x00 0x00 0x00 0x00 0x00 = 1.0
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Float64, 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        var result = Subject.Decode(buffer);

        result.Value.FloatValue.Should().Be(1.0);
        result.BytesConsumed.Should().Be(9);
    }

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        var buffer = ReadOnlySequence<byte>.Empty;

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnBufferTooShort()
    {
        var buffer =
            new ReadOnlySequence<byte>(
                [PackStreamMarker.Float64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]); // Missing eighth value byte

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        var buffer =
            new ReadOnlySequence<byte>(
                [0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]); // Null marker, not Float64

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<InvalidOperationException>();
    }
}
