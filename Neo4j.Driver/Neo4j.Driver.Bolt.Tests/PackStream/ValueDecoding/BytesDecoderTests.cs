using System.Buffers;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class BytesDecoderTests : DecoderTestsBase<BytesDecoder>
{
    [SetUp]
    public void SetUp()
    {
    }

    [Test]
    public void HandlesBytes8Bytes16Bytes32MarkerBytes()
    {
        Subject.HandledMarkerBytes.Should()
            .BeEquivalentTo([PackStreamMarker.Bytes8, PackStreamMarker.Bytes16, PackStreamMarker.Bytes32]);
    }

    [Test]
    public void DecodesBytes8()
    {
        // Bytes8: marker + 1 byte length (3) + 3 bytes data
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes8, 0x03, 0xAA, 0xBB, 0xCC]);

        var result = Subject.Decode(buffer);

        result.Value.BytesValue.ToArray().Should().BeEquivalentTo([0xAA, 0xBB, 0xCC]);
        result.BytesConsumed.Should().Be(5);
    }

    [Test]
    public void DecodesBytes8Empty()
    {
        // Bytes8: marker + 1 byte length (0)
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes8, 0x00]);

        var result = Subject.Decode(buffer);

        result.Value.BytesValue.ToArray().Should().BeEmpty();
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesBytes16()
    {
        // Bytes16: marker + 2 byte length (3) + 3 bytes data
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes16, 0x00, 0x03, 0xAA, 0xBB, 0xCC]);

        var result = Subject.Decode(buffer);

        result.Value.BytesValue.ToArray().Should().BeEquivalentTo([0xAA, 0xBB, 0xCC]);
        result.BytesConsumed.Should().Be(6);
    }

    [Test]
    public void DecodesBytes32()
    {
        // Bytes32: marker + 4 byte length (3) + 3 bytes data
        var buffer =
            new ReadOnlySequence<byte>([PackStreamMarker.Bytes32, 0x00, 0x00, 0x00, 0x03, 0xAA, 0xBB, 0xCC]);

        var result = Subject.Decode(buffer);

        result.Value.BytesValue.ToArray().Should().BeEquivalentTo([0xAA, 0xBB, 0xCC]);
        result.BytesConsumed.Should().Be(8);
    }

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        var buffer = ReadOnlySequence<byte>.Empty;

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnBytes8BufferTooShortForHeader()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes8]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnBytes8BufferTooShortForData()
    {
        var buffer =
            new ReadOnlySequence<byte>([PackStreamMarker.Bytes8, 0x05, 0xAA, 0xBB]); // Says 5 bytes, only 2

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnBytes16BufferTooShortForHeader()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes16, 0x00]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnBytes32BufferTooShortForHeader()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Bytes32, 0x00, 0x00, 0x00]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        var buffer = new ReadOnlySequence<byte>([0xC0, 0x03, 0xAA, 0xBB, 0xCC]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<InvalidOperationException>();
    }
}
