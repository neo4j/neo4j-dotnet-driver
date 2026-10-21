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
using Moq;
using NUnit.Framework;
using FluentAssertions;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Implementations;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.Tests.PackStream;

internal class PackStreamDecoderTests : UnitTestBase<PackStreamDecoder>
{
    [Test]
    public async Task DecodesSingleValue()
    {
        byte[] packStreamMessage = [0x01, 0x69, 0xEF];
        var byteReader = new Mock<IByteReader>();

        var dummyDecoder = new MockDecoder(
            [0x01],
            [0x01],
            PackStreamValueView.Integer(-123));
        
        AutoMocker.GetMock<IValueDecoderProvider>()
            .Setup(x => x.TryGetDecoder(It.IsAny<byte>(), It.IsAny<IPackStreamDecoder>(), out It.Ref<IValueDecoder?>.IsAny!))
            .Callback((byte _, IPackStreamDecoder _, out IValueDecoder? d) => { d = dummyDecoder; })
            .Returns(true);

        ReadOnlySequence<byte>[] messages = [new(packStreamMessage)];
        var chunkAssembler = AutoMocker.GetMock<IChunkAssembler>();
        chunkAssembler
            .Setup(x => x.ReadMessagesAsync(byteReader.Object, CancellationToken.None))
            .Returns(messages.ToAsyncEnumerable());

        var result = await Subject.Decode(byteReader.Object).Take(1).ToListAsync().ConfigureAwait(false);
        result.Should().HaveCount(1);
        result.First().Should().Be(PackStreamValueView.Integer(-123));
    }

    [Test]
    public async Task DecodesMultipleValues()
    {
        Dictionary<byte[], PackStreamValueView> packStreamMessages = new()
        {
            // not real packstream messages
            [[0x01,0x02, 0x03]] = PackStreamValueView.Integer(12345),
            [[0x32, 0xFF, 0xFF, 0xFF]] = PackStreamValueView.Integer(123456789),
            [[0xFF, 0x00]] = PackStreamValueView.Float(123.456)
        };

        foreach (var (bytes, packStreamValue) in packStreamMessages)
        {
            var decoder = new MockDecoder([bytes[0]], bytes, packStreamValue);
            AutoMocker.GetMock<IValueDecoderProvider>()
                .Setup(x => x.TryGetDecoder(bytes[0], It.IsAny<IPackStreamDecoder>(), out It.Ref<IValueDecoder?>.IsAny!))
                .Callback((byte _, IPackStreamDecoder _, out IValueDecoder? d) => { d = decoder; })
                .Returns(true);
        }

        var messages = packStreamMessages.Select(kvp => new ReadOnlySequence<byte>(kvp.Key)).ToArray();
        var chunkAssembler = AutoMocker.GetMock<IChunkAssembler>();
        var byteReader = new Mock<IByteReader>();
        chunkAssembler
            .Setup(x => x.ReadMessagesAsync(byteReader.Object, CancellationToken.None))
            .Returns(messages.ToAsyncEnumerable());

        var result = await Subject.Decode(byteReader.Object).Take(3).ToListAsync().ConfigureAwait(false);
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(packStreamMessages.Values);
    }

    [Test]
    public async Task DecodeFromStreamYieldsMultipleValuesFromOneChunk()
    {
        // One Bolt message chunk containing two PackStream values (0x01 and 0x02).
        // The stream Decode uses the sync Decode(ReadOnlySequence) in a loop, so we get two values.
        var decoderFor0x01 = new MockDecoder([0x01], [0x01], PackStreamValueView.Integer(10));
        var decoderFor0x02 = new MockDecoder([0x02], [0x02], PackStreamValueView.Integer(20));
        AutoMocker.GetMock<IValueDecoderProvider>()
            .Setup(x => x.TryGetDecoder(0x01, It.IsAny<IPackStreamDecoder>(), out It.Ref<IValueDecoder?>.IsAny!))
            .Callback((byte _, IPackStreamDecoder _, out IValueDecoder? d) => { d = decoderFor0x01; })
            .Returns(true);
        AutoMocker.GetMock<IValueDecoderProvider>()
            .Setup(x => x.TryGetDecoder(0x02, It.IsAny<IPackStreamDecoder>(), out It.Ref<IValueDecoder?>.IsAny!))
            .Callback((byte _, IPackStreamDecoder _, out IValueDecoder? d) => { d = decoderFor0x02; })
            .Returns(true);

        var byteReader = new Mock<IByteReader>();
        ReadOnlySequence<byte>[] chunks = [new ReadOnlySequence<byte>([0x01, 0x02])];
        AutoMocker.GetMock<IChunkAssembler>()
            .Setup(x => x.ReadMessagesAsync(byteReader.Object, CancellationToken.None))
            .Returns(chunks.ToAsyncEnumerable());

        var result = await Subject.Decode(byteReader.Object).Take(2).ToListAsync().ConfigureAwait(false);

        result.Should().HaveCount(2);
        result[0].IntValue.Should().Be(10);
        result[1].IntValue.Should().Be(20);
    }

    [Test]
    public void DecodeThrowsInvalidOperationExceptionWhenNoDecoderForMarkerByte()
    {
        AutoMocker.GetMock<IValueDecoderProvider>()
            .Setup(x => x.TryGetDecoder(0x99, It.IsAny<IPackStreamDecoder>(), out It.Ref<IValueDecoder?>.IsAny!))
            .Callback((byte _, IPackStreamDecoder _, out IValueDecoder? d) => { d = null; })
            .Returns(false);

        var buffer = new ReadOnlySequence<byte>([0x99, 0x00]);
        var byteReader = new Mock<IByteReader>();
        ReadOnlySequence<byte>[] chunks = [buffer];
        AutoMocker.GetMock<IChunkAssembler>()
            .Setup(x => x.ReadMessagesAsync(byteReader.Object, CancellationToken.None))
            .Returns(chunks.ToAsyncEnumerable());

        var act = async () => await Subject.Decode(byteReader.Object).Take(1).ToListAsync().ConfigureAwait(false);

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    private class MockDecoder : IValueDecoder
    {
        private readonly PackStreamValueView _decodeResult;
        private readonly int _messageLength;

        public MockDecoder(byte[] validMarkerBytes, IReadOnlyCollection<byte> message, PackStreamValueView decodeResult)
        {
            HandledMarkerBytes = validMarkerBytes;
            _decodeResult = decodeResult;
            _messageLength = message.Count;
        }

        public byte[] HandledMarkerBytes { get; }

        public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
        {
            HandledMarkerBytes.Should().Contain(buffer.First.Span[0]);
            return new ValueDecoderResult(_decodeResult, _messageLength);
        }
    }
}
