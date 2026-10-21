using System.Buffers;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using Neo4j.Driver.Bolt.Tests.TestHelpers;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class StringDecoderTests : DecoderTestsBase<StringDecoder>
{
    [Test]
    public void HandlesAllStringMarkerBytes()
    {
        var validBytes = new ByteArrayBuilder()
            .Range(0x80..0x90)
            .ExactBytes([PackStreamMarker.String8, PackStreamMarker.String16, PackStreamMarker.String32]);

        Subject.HandledMarkerBytes.Should().BeEquivalentTo(validBytes);
    }

    [Test]
    public void DecodesTinyStringEmpty()
    {
        var result = Subject.Decode(new ReadOnlySequence<byte>([0x80]));

        result.Value.StringValue.ToString().Should().BeEmpty();
        result.BytesConsumed.Should().Be(1);
    }

    [Test]
    public void DecodesTinyString()
    {
        var result = Subject.Decode(new ReadOnlySequence<byte>([0x85, .."hello"u8]));

        result.Value.StringValue.ToString().Should().Be("hello");
        result.BytesConsumed.Should().Be(6);
    }

    [Test]
    public void DecodesString8()
    {
        var result = Subject.Decode(new ReadOnlySequence<byte>([PackStreamMarker.String8, 0x05, .."hello"u8]));

        result.Value.StringValue.ToString().Should().Be("hello");
        result.BytesConsumed.Should().Be(7);
    }

    [Test]
    public void DecodesString16()
    {
        var result = Subject.Decode(
            new ReadOnlySequence<byte>([PackStreamMarker.String16, 0x00, 0x05, .."hello"u8]));

        result.Value.StringValue.ToString().Should().Be("hello");
        result.BytesConsumed.Should().Be(8);
    }

    [Test]
    public void DecodesString32()
    {
        var result = Subject.Decode(
            new ReadOnlySequence<byte>([PackStreamMarker.String32, 0x00, 0x00, 0x00, 0x05, .."hello"u8]));

        result.Value.StringValue.ToString().Should().Be("hello");
        result.BytesConsumed.Should().Be(10);
    }

    [Test]
    public void DecodesUtf8MultiByteCharacters()
    {
        var result = Subject.Decode(new ReadOnlySequence<byte>([0x85, .."café"u8])); // é is 2 bytes

        result.Value.StringValue.ToString().Should().Be("café");
        result.BytesConsumed.Should().Be(6);
    }

    [Test]
    public void StringValueSupportsEnumeration()
    {
        var result = Subject.Decode(new ReadOnlySequence<byte>([0x82, .."hi"u8]));

        var runes = new List<string>();
        foreach (var rune in result.Value.StringValue)
        {
            runes.Add(rune.ToString());
        }

        runes.Should().BeEquivalentTo(["h", "i"]);
    }

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        Action act = () => Subject.Decode(ReadOnlySequence<byte>.Empty);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnTruncatedData()
    {
        Action act = () => Subject.Decode(new ReadOnlySequence<byte>([0x85, 0x68, 0x69])); // Says 5 bytes, only 2

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        Action act = () => Subject.Decode(new ReadOnlySequence<byte>([0xC0, .."hello"u8]));

        act.Should().Throw<InvalidOperationException>();
    }
}
