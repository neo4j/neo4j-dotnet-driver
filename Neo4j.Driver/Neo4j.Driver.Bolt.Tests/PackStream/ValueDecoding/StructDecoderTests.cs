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
using System.Text;
using FluentAssertions;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using Neo4j.Driver.Bolt.Tests.TestHelpers;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using NUnit.Framework;

namespace Neo4j.Driver.Bolt.Tests.PackStream.ValueDecoding;

[TestFixture]
internal class StructDecoderTests : DecoderTestsBase<StructDecoder>
{
    [SetUp]
    public void SetUp()
    {
        Subject.SetRecursionDecoder(new MockPackStreamDecoder(Subject));
    }

    [Test]
    public void HandlesAllStructMarkerBytes()
    {
        var validBytes = new ByteArrayBuilder()
            .Range(0xB0, 0x10)
            .ExactBytes([PackStreamMarker.Struct8, PackStreamMarker.Struct16]);

        Subject.HandledMarkerBytes.Should().BeEquivalentTo(validBytes);
    }

    [Test]
    public void DecodesTinyStructEmpty()
    {
        // TinyStruct 0 fields, tag 0x2A
        var buffer = new ReadOnlySequence<byte>([0xB0, 0x2A]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x2A);
        result.Value.StructValue.Fields.Count.Should().Be(0);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesStruct8Empty()
    {
        // Struct8 with 0 fields, tag 0x01
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Struct8, 0x00, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x01);
        result.Value.StructValue.Fields.Count.Should().Be(0);
        result.BytesConsumed.Should().Be(3);
    }

    [Test]
    public void DecodesTinyStructWithOneField()
    {
        // TinyStruct 1 field, tag 0x2A, field = 0x01 (int 1)
        var buffer = new ReadOnlySequence<byte>([0xB1, 0x2A, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x2A);
        result.Value.StructValue.Fields.Count.Should().Be(1);
        result.BytesConsumed.Should().Be(3);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields[0].IntValue.Should().Be(1);
    }

    [Test]
    public void DecodesTinyStructWithMultipleFields()
    {
        // TinyStruct 2 fields, tag 0x2A, fields = 0x01, 0x02
        var buffer = new ReadOnlySequence<byte>([0xB2, 0x2A, 0x01, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x2A);
        result.Value.StructValue.Fields.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(4);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields[0].IntValue.Should().Be(1);
        fields[1].IntValue.Should().Be(2);
    }

    [Test]
    public void DecodesTinyStructWithGetEnumerator()
    {
        var buffer = new ReadOnlySequence<byte>([0xB2, 0x2A, 0x01, 0x02]);

        var result = Subject.Decode(buffer);

        var fields = new List<PackStreamValueView>();
        foreach (var f in result.Value.StructValue.Fields)
        {
            fields.Add(f);
        }

        fields.Should().HaveCount(2);
        fields[0].IntValue.Should().Be(1);
        fields[1].IntValue.Should().Be(2);
    }

    [Test]
    public void DecodesTinyStructMaxSize()
    {
        // TinyStruct 15 fields (max), tag 0x00, 15 single-byte int fields
        var bytes = new List<byte> { 0xBF, 0x00 };
        for (var i = 0; i < 15; i++)
        {
            bytes.Add((byte)(0x01 + (i % 5)));
        }

        var buffer = new ReadOnlySequence<byte>(bytes.ToArray());
        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x00);
        result.Value.StructValue.Fields.Count.Should().Be(15);
        result.BytesConsumed.Should().Be(17);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields.Should().HaveCount(15);
    }

    [Test]
    public void DecodesStruct8()
    {
        // Struct8 with 2 fields, tag 0x10, fields 0x01, 0x02
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Struct8, 0x02, 0x10, 0x01, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x10);
        result.Value.StructValue.Fields.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(5);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields[0].IntValue.Should().Be(1);
        fields[1].IntValue.Should().Be(2);
    }

    [Test]
    public void DecodesStruct16()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Struct16, 0x00, 0x02, 0x20, 0x01, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x20);
        result.Value.StructValue.Fields.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(6);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields[0].IntValue.Should().Be(1);
        fields[1].IntValue.Should().Be(2);
    }

    [Test]
    public void DecodesStructWithMixedFieldTypes()
    {
        // 2 fields: int 1, string "Hello"
        var buffer = new ReadOnlySequence<byte>([0xB2, 0x2A, 0x01, 0x20]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Fields.Count.Should().Be(2);

        var fields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        fields[0].IntValue.Should().Be(1);
        fields[1].StringValue.ToString().Should().Be("Hello");
    }

    [Test]
    public void DecodesNestedStruct()
    {
        // Outer struct: tag 0x01, 1 field = inner struct (tag 0x02, 1 field = int 1)
        // B1 0x01 B1 0x02 0x01
        var buffer = new ReadOnlySequence<byte>([0xB1, 0x01, 0xB1, 0x02, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.StructValue.Tag.Should().Be(0x01);
        result.Value.StructValue.Fields.Count.Should().Be(1);
        result.BytesConsumed.Should().Be(5);

        var outerFields = result.Value.StructValue.Fields.ToEnumerable().ToList();
        outerFields[0].Type.Should().Be(PackStreamType.Struct);
        outerFields[0].StructValue.Tag.Should().Be(0x02);
        outerFields[0].StructValue.Fields.Count.Should().Be(1);
        outerFields[0].StructValue.Fields.ToEnumerable().First().IntValue.Should().Be(1);
    }

    [Test]
    public void ThrowsOnEmptyBuffer()
    {
        Action act = () => Subject.Decode(ReadOnlySequence<byte>.Empty);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnUnknownMarker()
    {
        Action act = () => Subject.Decode(new ReadOnlySequence<byte>([PackStreamMarker.Null]));

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ThrowsOnStruct8WithInsufficientHeaderBytes()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Struct8]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnTinyStructWithInsufficientData()
    {
        // TinyStruct claims 1 field but no field bytes (only marker + tag)
        var buffer = new ReadOnlySequence<byte>([0xB1, 0x2A]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnNestedStructWithInsufficientData()
    {
        // Outer struct 1 field, value is struct with 1 field but no data
        var buffer = new ReadOnlySequence<byte>([0xB1, 0x01, 0xB1, 0x02]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void FieldsToEnumerableSupportsLinq()
    {
        var buffer = new ReadOnlySequence<byte>([0xB3, 0x2A, 0x01, 0x02, 0x03]);

        var result = Subject.Decode(buffer);

        var sum = result.Value.StructValue.Fields.ToEnumerable().Sum(v => v.IntValue);

        sum.Should().Be(6);
    }

    private class MockPackStreamDecoder : IPackStreamDecoder
    {
        private readonly IRecursiveValueDecoder _structDecoder;

        public MockPackStreamDecoder(IRecursiveValueDecoder structDecoder)
        {
            _structDecoder = structDecoder;
            _structDecoder.SetRecursionDecoder(this);
        }

        public IAsyncEnumerable<PackStreamValueView> Decode(IByteReader byteReader, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        private static ReadOnlySequence<byte> GetUtfBytes(string str) => new(Encoding.UTF8.GetBytes(str));

        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
        {
            var array = buffer.ToArray();
            var marker = array[0];
            return marker switch
            {
                0x01 => new ValueDecoderResult(PackStreamValueView.Integer(1), 1),
                0x02 => new ValueDecoderResult(PackStreamValueView.Integer(2), 1),
                0x03 => new ValueDecoderResult(PackStreamValueView.Integer(3), 1),
                0x04 => new ValueDecoderResult(PackStreamValueView.Integer(4), 1),
                0x05 => new ValueDecoderResult(PackStreamValueView.Integer(5), 1),
                0x11 => new ValueDecoderResult(PackStreamValueView.Float(0.1f), 1),
                0x12 => new ValueDecoderResult(PackStreamValueView.Float(0.2f), 1),
                0x20 => new ValueDecoderResult(PackStreamValueView.String(GetUtfBytes("Hello")), 1),
                0x21 => new ValueDecoderResult(PackStreamValueView.String(GetUtfBytes("World")), 1),
                >= 0xB0 and <= 0xBF or PackStreamMarker.Struct8 or PackStreamMarker.Struct16
                    => _structDecoder.Decode(buffer),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(buffer),
                    $"No mock value defined for marker: 0x{marker:X2}")
            };
        }
    }
}
