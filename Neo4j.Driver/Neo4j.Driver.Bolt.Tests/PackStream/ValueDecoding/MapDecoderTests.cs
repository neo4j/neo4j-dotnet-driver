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
internal class MapDecoderTests : DecoderTestsBase<MapDecoder>
{
    [SetUp]
    public void SetUp()
    {
        Subject.SetRecursionDecoder(new MockPackStreamDecoder(Subject));
    }

    [Test]
    public void HandlesAllMapMarkerBytes()
    {
        var validBytes = new ByteArrayBuilder()
            .Range(0xA0, 0x10)
            .ExactBytes([PackStreamMarker.Map8, PackStreamMarker.Map16, PackStreamMarker.Map32]);

        Subject.HandledMarkerBytes.Should().BeEquivalentTo(validBytes);
    }

    [Test]
    public void DecodesTinyMapEmpty()
    {
        var buffer = new ReadOnlySequence<byte>([0xA0]); // TinyMap with 0 entries

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(0);
        result.BytesConsumed.Should().Be(1);
    }

    [Test]
    public void DecodesMap8Empty()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Map8, 0x00]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(0);
        result.BytesConsumed.Should().Be(2);
    }

    [Test]
    public void DecodesTinyMapWithOneEntry()
    {
        // TinyMap with 1 entry: key "Hello" (0x20), value 1 (0x01)
        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(1);
        result.BytesConsumed.Should().Be(3);

        var entries = result.Value.MapValue.ToEnumerable()
            .ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value.IntValue);

        entries.Should().HaveCount(1);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(1);
    }

    [Test]
    public void DecodesTinyMapWithMultipleEntries()
    {
        // TinyMap with 2 entries: "Hello"=>1, "World"=>2
        var buffer = new ReadOnlySequence<byte>([0xA2, 0x20, 0x01, 0x21, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(5); // A2 (1) + 4 payload bytes

        var entries = result.Value.MapValue.ToEnumerable()
            .ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value.IntValue);

        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(1);
        entries.Should().ContainKey("World").WhoseValue.Should().Be(2);
    }

    [Test]
    public void DecodesTinyMapWithGetEnumerator()
    {
        var buffer = new ReadOnlySequence<byte>([0xA2, 0x20, 0x01, 0x21, 0x02]);

        var result = Subject.Decode(buffer);

        var entries = new Dictionary<string, long>();
        foreach (var entry in result.Value.MapValue)
        {
            entries.Add(entry.Key.StringValue.ToString(), entry.Value.IntValue);
        }

        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(1);
        entries.Should().ContainKey("World").WhoseValue.Should().Be(2);
    }

    [Test]
    public void DecodesTinyMapMaxSize()
    {
        // TinyMap with 15 entries (max for TinyMap): {"Hello": 1, "Hello": 2, ...}
        var bytes = new List<byte> { 0xAF }; // TinyMap 15 entries
        for (var i = 0; i < 15; i++)
        {
            bytes.Add(0x20); // key "Hello"
            bytes.Add((byte)(i % 5 + 1));
        }

        var buffer = new ReadOnlySequence<byte>(bytes.ToArray());
        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(15);
        result.BytesConsumed.Should().Be(31);

        var entries = result.Value.MapValue.ToEnumerable().ToList();
        entries.Should().HaveCount(15);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            entry.Key.StringValue.ToString().Should().Be("Hello");
            entry.Value.IntValue.Should().Be((byte)(i % 5 + 1));
        }
    }

    [Test]
    public void DecodesMap8()
    {
        // Map8 with 2 entries: "Hello"=>5, "World"=>6
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Map8, 0x02, 0x20, 0x05, 0x21, 0x03]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(6);

        var entries = result.Value.MapValue.ToEnumerable()
            .ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value.IntValue);

        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(5);
        entries.Should().ContainKey("World").WhoseValue.Should().Be(3);
    }

    [Test]
    public void DecodesMap16()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Map16, 0x00, 0x02, 0x20, 0x01, 0x21, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(7); // D9 (1) + 2 size bytes + 4 payload

        var entries = result.Value.MapValue.ToEnumerable()
            .ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value.IntValue);

        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(1);
        entries.Should().ContainKey("World").WhoseValue.Should().Be(2);
    }

    [Test]
    public void DecodesMap32()
    {
        var buffer = new ReadOnlySequence<byte>(
            [PackStreamMarker.Map32, 0x00, 0x00, 0x00, 0x02, 0x20, 0x01, 0x21, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(2);
        result.BytesConsumed.Should().Be(9); // DA (1) + 4 size bytes + 4 payload

        var entries = result.Value.MapValue.ToEnumerable()
            .ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value.IntValue);
        
        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(1);
        entries.Should().ContainKey("World").WhoseValue.Should().Be(2);
    }

    [Test]
    public void DecodesMapWithMixedValueTypes()
    {
        // 2 entries: "Hello"=>1 (int), "World"=>0.1 (float)
        var buffer = new ReadOnlySequence<byte>([0xA2, 0x20, 0x01, 0x21, 0x11]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(2);

        var entries = result.Value.MapValue.ToEnumerable().ToDictionary(e => e.Key.StringValue.ToString(), e => e.Value);
        entries.Should().HaveCount(2);
        entries.Should().ContainKey("Hello").WhoseValue.Should().Be(PackStreamValueView.Integer(1));
        entries["World"].FloatValue.Should().BeApproximately(0.1f, 0.00001f);
    }

    [Test]
    public void DecodesNestedMap()
    {
        // Outer map 1 entry: key "Hello", value = inner map {"World": 1}
        // A1 = TinyMap(1), 0x20 = "Hello", A1 = TinyMap(1), 0x21 = "World", 0x01 = 1
        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20, 0xA1, 0x21, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(1);
        result.BytesConsumed.Should().Be(5);

        var outerEntries = result.Value.MapValue.ToEnumerable().ToList();
        outerEntries.Should().HaveCount(1);
        outerEntries[0].Key.StringValue.ToString().Should().Be("Hello");
        outerEntries[0].Value.Type.Should().Be(PackStreamType.Map);

        var innerMap = outerEntries[0].Value.MapValue;
        innerMap.Count.Should().Be(1);
        var innerEntries = innerMap.ToEnumerable().ToList();
        innerEntries[0].Key.StringValue.ToString().Should().Be("World");
        innerEntries[0].Value.IntValue.Should().Be(1);
    }

    [Test]
    public void DecodesNestedMapWithMultipleLevels()
    {
        // Outer: one entry "a" => middle map; middle: one entry "b" => inner map; inner: one entry "c" => 1
        // A1 0x20 A1 0x21 A1 0x20 0x01
        // "Hello" => {"World" => {"Hello" => 1}}
        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20, 0xA1, 0x21, 0xA1, 0x20, 0x01]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(1);
        var level1 = result.Value.MapValue.ToEnumerable().ToList();
        level1[0].Key.StringValue.ToString().Should().Be("Hello");
        var level2 = level1[0].Value.MapValue.ToEnumerable().ToList();
        level2[0].Key.StringValue.ToString().Should().Be("World");
        var level3 = level2[0].Value.MapValue.ToEnumerable().ToList();
        level3[0].Key.StringValue.ToString().Should().Be("Hello");
        level3[0].Value.IntValue.Should().Be(1);
    }

    [Test]
    public void DecodesMapWithListValue()
    {
        // Map 1 entry: key "Hello" (0x20), value list [1, 2] (0x92 0x01 0x02)
        // Mock delegates map markers to MapDecoder and list markers to ListDecoder.
        var logger = AutoMocker.Get<Microsoft.Extensions.Logging.ILogger>();
        var listDecoder = new ListDecoder(logger);
        var combinedMock = new MockPackStreamDecoderForMapAndList(Subject, listDecoder);
        Subject.SetRecursionDecoder(combinedMock);
        listDecoder.SetRecursionDecoder(combinedMock);

        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20, 0x92, 0x01, 0x02]);

        var result = Subject.Decode(buffer);

        result.Value.MapValue.Count.Should().Be(1);
        var entries = result.Value.MapValue.ToEnumerable().ToList();
        entries[0].Key.StringValue.ToString().Should().Be("Hello");
        entries[0].Value.Type.Should().Be(PackStreamType.List);
        entries[0].Value.ListValue.Count.Should().Be(2);
        entries[0].Value.ListValue.ToEnumerable().Select(v => v.IntValue).Should().BeEquivalentTo([1, 2]);
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
    public void ThrowsOnMap8WithInsufficientHeaderBytes()
    {
        var buffer = new ReadOnlySequence<byte>([PackStreamMarker.Map8]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnTinyMapWithInsufficientData()
    {
        // TinyMap claims 1 entry but only has key, no value
        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ThrowsOnNestedMapWithInsufficientData()
    {
        // Map 1 entry: key "Hello", value is nested map that claims 1 entry but has no data
        var buffer = new ReadOnlySequence<byte>([0xA1, 0x20, 0xA1]);

        Action act = () => Subject.Decode(buffer);

        act.Should().Throw<ProtocolException>();
    }

    [Test]
    public void ToEnumerableSupportsLinq()
    {
        var buffer = new ReadOnlySequence<byte>([0xA2, 0x20, 0x01, 0x21, 0x02]);

        var result = Subject.Decode(buffer);

        var values = result.Value.MapValue.ToEnumerable()
            .Select(e => e.Value.IntValue)
            .ToList();

        values.Should().BeEquivalentTo([1, 2]);
    }

    private class MockPackStreamDecoder : IPackStreamDecoder
    {
        private readonly IRecursiveValueDecoder _mapDecoder;

        public MockPackStreamDecoder(IRecursiveValueDecoder mapDecoder)
        {
            _mapDecoder = mapDecoder;
            _mapDecoder.SetRecursionDecoder(this);
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
                >= 0xA0 and <= 0xAF or PackStreamMarker.Map8 or PackStreamMarker.Map16 or PackStreamMarker.Map32
                    => _mapDecoder.Decode(buffer),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(buffer),
                    $"No mock value defined for marker: 0x{marker:X2}")
            };
        }
    }

    /// <summary>
    /// Mock that delegates both map and list markers so we can test map with list values.
    /// </summary>
    private class MockPackStreamDecoderForMapAndList : IPackStreamDecoder
    {
        private readonly IRecursiveValueDecoder _mapDecoder;
        private readonly IRecursiveValueDecoder _listDecoder;

        public MockPackStreamDecoderForMapAndList(
            IRecursiveValueDecoder mapDecoder,
            IRecursiveValueDecoder listDecoder)
        {
            _mapDecoder = mapDecoder;
            _listDecoder = listDecoder;
            _mapDecoder.SetRecursionDecoder(this);
            _listDecoder.SetRecursionDecoder(this);
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
                0x20 => new ValueDecoderResult(PackStreamValueView.String(GetUtfBytes("Hello")), 1),
                0x21 => new ValueDecoderResult(PackStreamValueView.String(GetUtfBytes("World")), 1),
                >= 0xA0 and <= 0xAF or PackStreamMarker.Map8 or PackStreamMarker.Map16 or PackStreamMarker.Map32
                    => _mapDecoder.Decode(buffer),
                >= 0x90 and <= 0x9F or PackStreamMarker.List8 or PackStreamMarker.List16 or PackStreamMarker.List32
                    => _listDecoder.Decode(buffer),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(buffer),
                    $"No mock value defined for marker: 0x{marker:X2}")
            };
        }
    }
}
