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

using System.IO.Pipelines;
using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Implementations;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;
using Neo4j.Driver.Bolt.Tests.Transport;
using Neo4j.Driver.Bolt.Transport.Abstractions;
using Neo4j.Driver.Bolt.Transport.Implementations;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;

namespace Neo4j.Driver.Bolt.Tests.PackStream.Integration;

/// <summary>
/// Integration tests that exercise the full pipeline: byte reader → chunk assembler → PackStream decoder.
/// Wire format is Bolt message framing: each message is a 2-byte big-endian size followed by the message body (PackStream).
/// </summary>
[TestFixture]
internal class PackStreamDecoderChunkAssemblyIntegrationTests
{
    private AutoMocker _autoMocker = new();
    private IPackStreamDecoder Subject => _autoMocker.CreateInstance<PackStreamDecoder>();

    [SetUp]
    public void SetUp()
    {
        _autoMocker = new AutoMocker();
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();

        var frameworkLogger = new SerilogLoggerProvider(logger).CreateLogger("Neo4j.Driver.Bolt.Tests");
        _autoMocker.Use(frameworkLogger);

        var decoders = new IValueDecoder[]
        {
            _autoMocker.CreateInstance<NullDecoder>(),
            _autoMocker.CreateInstance<BooleanDecoder>(),
            _autoMocker.CreateInstance<TinyIntDecoder>(),
            _autoMocker.CreateInstance<IntegerDecoder>(),
            _autoMocker.CreateInstance<FloatDecoder>(),
            _autoMocker.CreateInstance<StringDecoder>(),
            _autoMocker.CreateInstance<BytesDecoder>(),
            _autoMocker.CreateInstance<ListDecoder>(),
            _autoMocker.CreateInstance<MapDecoder>(),
            _autoMocker.CreateInstance<StructDecoder>(),
        };

        _autoMocker.Use(decoders);
        var provider = new ValueDecoderProvider(decoders, frameworkLogger);
        _autoMocker.Use<IValueDecoderProvider>(provider);

        // Use the real chunk assembler so we test the full pipeline.
        var chunkAssembler = new ChunkAssembler(frameworkLogger);
        _autoMocker.Use<IChunkAssembler>(chunkAssembler);
    }

    /// <summary>
    /// Feeds bytes in chunks to simulate the byte reader receiving data in multiple reads.
    /// </summary>
    private static async Task<IByteReader> CreateChunkedByteReader(IEnumerable<byte[]> chunks)
    {
        var pipe = new Pipe();
        var reader = new PipeReaderByteReader(pipe.Reader);

        _ = Task.Run(async () =>
        {
            foreach (var chunk in chunks)
            {
                await pipe.Writer.WriteAsync(chunk).ConfigureAwait(false);
            }

            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        });

        return reader;
    }

    [Test]
    public async Task DecodesSingleValueInOneMessageWithChunkAssembly()
    {
        // One Bolt message: the message size is 4 (big-endian short). The message body is one PackStream value.
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is given as a 2-byte big-endian integer (3 bytes).
            0x00, 0x03,
            // Message body (PackStream):
            //   This is a list; the number of items is in the low nibble of the marker (2 items).
            0x92,
            //   First item: a tiny int, value 10
            0x0A,
            //   Second item: a tiny int, value 20
            0x14,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        var materialisedListItems = new List<long>();
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            value.Type.Should().Be(PackStreamType.List);
            value.ListValue.Count.Should().Be(2);
            materialisedListItems.AddRange(value.ListValue.ToEnumerable().Select(v => v.IntValue));
        }

        valueCount.Should().Be(1);
        materialisedListItems.Should().BeEquivalentTo([10L, 20L]);
    }

    [Test]
    public async Task DecodesMultipleValuesInOneMessage()
    {
        // One Bolt message containing three PackStream values: an int, a string, and a list.
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 10 bytes (big-endian).
            0x00, 0x0A,
            // Message body:
            //   First value: a tiny int, 42
            0x2A,
            //   Second value: a string; the length is in the low nibble (5 bytes), then UTF-8 "hello"
            0x85, 0x68, 0x65, 0x6C, 0x6C, 0x6F,
            //   Third value: a list of 2 items (tiny ints 1, 2)
            0x92, 0x01, 0x02,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        long? materialisedInt = null;
        string? materialisedString = null;
        List<long>? materialisedListItems = null;
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            switch (valueCount)
            {
                case 1:
                    materialisedInt = value.IntValue;
                    break;
                case 2:
                    materialisedString = value.StringValue.ToString();
                    break;
                case 3:
                    materialisedListItems = value.ListValue.ToEnumerable().Select(v => v.IntValue).ToList();
                    break;
            }
        }

        valueCount.Should().Be(3);
        materialisedInt.Should().Be(42);
        materialisedString.Should().Be("hello");
        materialisedListItems.Should().BeEquivalentTo([1L, 2L]);
    }

    [Test]
    public async Task DecodesValuesSplitAcrossTwoBoltMessages()
    {
        // First Bolt message: one PackStream value (a tiny int). Second message: one value (a string).
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 1 byte.
            0x00, 0x01,
            // Message body: a single tiny int, value 7
            0x07,
            // -------- Bolt message 2 --------
            // The length of the message body is 6 bytes (big-endian).
            0x00, 0x06,
            // Message body: a string; length in low nibble (5), then UTF-8 "world"
            0x85, 0x77, 0x6F, 0x72, 0x6C, 0x64,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        long? materialisedInt = null;
        string? materialisedString = null;
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            if (valueCount == 1)
            {
                materialisedInt = value.IntValue;
            }
            else
            {
                materialisedString = value.StringValue.ToString();
            }
        }

        valueCount.Should().Be(2);
        materialisedInt.Should().Be(7);
        materialisedString.Should().Be("world");
    }

    [Test]
    public async Task DecodesNestedListAndMapInOneMessage()
    {
        // One Bolt message whose body is a single PackStream list containing a map.
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 9 bytes (big-endian).
            0x00, 0x09,
            // Message body: one value, a list of 2 items
            //   List marker: TinyList, 2 items
            0x92,
            //   First item: a tiny int, 1
            0x01,
            //   Second item: a map; the entry count is in the low nibble (2 entries)
            0xA2,
            //     First entry: key string "a", value tiny int 10
            0x81, 0x61, 0x0A,
            //     Second entry: key string "b", value tiny int 20
            0x81, 0x62, 0x14,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        long? firstListItem = null;
        var mapEntries = new List<(string Key, long Value)>();
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            var list = value.ListValue.ToEnumerable().ToList();
            firstListItem = list[0].IntValue;
            foreach (var entry in list[1].MapValue.ToEnumerable())
            {
                var keyStr = entry.Key.StringValue.ToString();
                var val = entry.Value.IntValue;
                mapEntries.Add((keyStr, val));
            }
        }

        valueCount.Should().Be(1);
        firstListItem.Should().Be(1);
        mapEntries.Should().HaveCount(2);
        mapEntries[0].Key.Should().Be("a");
        mapEntries[0].Value.Should().Be(10);
        mapEntries[1].Key.Should().Be("b");
        mapEntries[1].Value.Should().Be(20);
    }

    [Test]
    public async Task DecodesMessageWhenBodyArrivesInTwoReads()
    {
        // Simulate the byte reader returning the chunk header and part of the body in one read,
        // and the rest of the body in a second read. The chunk assembler must wait for the full message.
        // The list's payload is a ReadOnlySequence from the pipe; we must materialise it while
        // enumerating decoded values so we do not hold a reference after the pipe advances.
        byte[] chunk1 =
        [
            // -------- Bolt message 1 (header + partial body) --------
            // The length of the message body is 4 bytes (big-endian).
            0x00, 0x04,
            // First 3 bytes of the message body: a list of 3 items, first two items
            0x93, 0x01, 0x02,
        ];

        byte[] chunk2 =
        [
            // Remaining 1 byte of the message body: third item (tiny int 3)
            0x03,
        ];

        var byteReader = await CreateChunkedByteReader([chunk1, chunk2]).ConfigureAwait(false);
        var materialisedListItems = new List<long>();
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            value.Type.Should().Be(PackStreamType.List);
            value.ListValue.Count.Should().Be(3);
            // Materialise the list while the underlying ReadOnlySequence is still valid.
            materialisedListItems.AddRange(value.ListValue.ToEnumerable().Select(v => v.IntValue));
        }

        valueCount.Should().Be(1);
        materialisedListItems.Should().BeEquivalentTo([1L, 2L, 3L]);
    }

    [Test]
    public async Task DecodesTwoMessagesWhenSecondMessageArrivesInLaterRead()
    {
        // First read: complete first message (one value). Second read: second message (one value).
        byte[] chunk1 =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 1 byte.
            0x00, 0x01,
            // Message body: boolean true
            0xC3,
        ];

        byte[] chunk2 =
        [
            // -------- Bolt message 2 --------
            // The length of the message body is 1 byte.
            0x00, 0x01,
            // Message body: boolean false
            0xC2,
        ];

        var byteReader = await CreateChunkedByteReader([chunk1, chunk2]).ConfigureAwait(false);
        var values = await Subject.Decode(byteReader).Take(2).ToListAsync().ConfigureAwait(false);

        values.Should().HaveCount(2);
        values[0].BooleanValue.Should().BeTrue();
        values[1].BooleanValue.Should().BeFalse();
    }

    [Test]
    public async Task DecodesNullAndStructInOneMessage()
    {
        // One Bolt message: two PackStream values, null and a struct.
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 5 bytes (big-endian).
            0x00, 0x05,
            // First value: null (single marker byte)
            0xC0,
            // Second value: a structure; the field count is in the low nibble (2 fields), then the tag byte
            0xB2, 0x4E,
            // First field: tiny int 1
            0x01,
            // Second field: tiny int 2
            0x02,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        var firstWasNull = false;
        byte? materialisedStructTag = null;
        List<long>? materialisedStructFields = null;
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            if (valueCount == 1)
            {
                firstWasNull = value.IsNull;
            }
            else
            {
                materialisedStructTag = value.StructValue.Tag;
                materialisedStructFields = value.StructValue.Fields.ToEnumerable().Select(v => v.IntValue).ToList();
            }
        }

        valueCount.Should().Be(2);
        firstWasNull.Should().BeTrue();
        materialisedStructTag.Should().Be(0x4E);
        materialisedStructFields.Should().BeEquivalentTo([1L, 2L]);
    }

    [Test]
    public async Task DecodesEmptyMessageThenValueMessage()
    {
        // First message has zero-length body; second message has one PackStream value.
        byte[] wire =
        [
            // -------- Bolt message 1 --------
            // The length of the message body is 0 bytes (big-endian).
            0x00, 0x00,
            // -------- Bolt message 2 --------
            // The length of the message body is 2 bytes (big-endian).
            0x00, 0x02,
            // Message body: a list of 1 item (tiny int 99)
            0x91, 0x63,
        ];

        var byteReader = TestByteReaders.FromSingleReadBuffer(wire);
        long? materialisedListItem = null;
        var valueCount = 0;

        await foreach (var value in Subject.Decode(byteReader))
        {
            valueCount++;
            value.ListValue.Count.Should().Be(1);
            materialisedListItem = value.ListValue.ToEnumerable().First().IntValue;
        }

        valueCount.Should().Be(1);
        materialisedListItem.Should().Be(99);
    }
}
