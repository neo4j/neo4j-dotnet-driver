using System.Buffers;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class NullDecoderTests : DecoderTestsBase<NullDecoder>
{
    [Test]
    public void HandlesNullMarkerByte()
    {
        Subject.HandledMarkerBytes.Should().BeEquivalentTo([PackStreamMarker.Null]);
    }

    [Test]
    public void DecodesNull()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Null]);

        var result = Subject.Decode(buffer);

        result.Value.IsNull.Should().BeTrue();
        result.BytesConsumed.Should().Be(1);
    }

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
        var buffer = new ReadOnlySequence<byte>([0x00]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<InvalidOperationException>();
    }
}
