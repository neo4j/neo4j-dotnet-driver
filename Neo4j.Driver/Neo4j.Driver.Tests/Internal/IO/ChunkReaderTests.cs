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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.Internal.IO.Utils;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO;

[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
public class ChunkReaderTests
{
    private const string CompactingArgumentRegEx = "bytes left in chunk buffer.*compacting\\.$";

    [Fact]
    public void ShouldThrowWhenConstructedUsingNullStream()
    {
        var ex = Record.Exception(() => new ChunkReader(null));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ShouldThrowWhenConstructedUsingNullStreamWithLogger()
    {
        var ex = Record.Exception(() => new ChunkReader(null));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ShouldThrowWhenConstructedUsingUnreadableStream()
    {
        var stream = new Mock<Stream>();
        stream.Setup(l => l.CanRead).Returns(false);

        var ex = Record.Exception(() => new ChunkReader(stream.Object));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(
        new byte[] { 0x00, 0x00 },
        new byte[] {},
        0)]
    [InlineData(
        new byte[]
        {
            0x00, 0x01, 0x00,
            0x00, 0x02, 0x01, 0x02,
            0x00, 0x00
        },
        new byte[] { 0x00, 0x01, 0x02 },
        1)]
    [InlineData(
        new byte[]
        {
            0x00, 0x01, 0x00,
            0x00, 0x01, 0x01,
            0x00, 0x01, 0x02,
            0x00, 0x00
        },
        new byte[] { 0x00, 0x01, 0x02 },
        1)]
    [InlineData(
        new byte[]
        {
            0x00, 0x01, 0x00,
            0x00, 0x01, 0x01,
            0x00, 0x01, 0x02,
            0x00, 0x00,
            0x00, 0x03, 0x00, 0x01, 0x02,
            0x00, 0x00
        },
        new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 },
        2)]
    public async void ShouldReadMessageSpanningMultipleChunksAsync(
        byte[] input,
        byte[] expectedMessageBuffers,
        int expectedCount)
    {
        var reader = new ChunkReader(new MemoryStream(input));

        var targetStream = new MemoryStream();
        var count = await reader.ReadMessageChunksToBufferStreamAsync(targetStream);
        var messageBuffers = targetStream.ToArray();

        count.Should().Be(expectedCount);
        messageBuffers.Should().Equal(expectedMessageBuffers);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })] //Half chunk
    [InlineData(new byte[] { 0x00, 0x01 })]
    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02 })]
    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01 })]
    public async void ShouldThrowWhenEndOfStreamIsDetectedAsync(byte[] input)
    {
        var reader = new ChunkReader(new MemoryStream(input));

        var targetStream = new MemoryStream();
        var ex = await Record.ExceptionAsync(() => reader.ReadMessageChunksToBufferStreamAsync(targetStream));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<IOException>().Which.Message.Should().StartWith("Unexpected end of stream");
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x00, 0x00, //NOOP
            0x00, 0x01, 0x00, //Message multichunk
            0x00, 0x01, 0x01,
            0x00, 0x01, 0x02,
            0x00, 0x00, //End of message
            0x00, 0x00, //NOOP
            0x00, 0x00, //NOOP
            0x00, 0x03, 0x00, 0x01, 0x02, //Message single chunk
            0x00, 0x00, //End of message
            0x00, 0x00
        }, //NOOP
        new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 },
        2)]
    public async void ShouldReadNoopsBetweenMessagesAsync(
        byte[] input,
        byte[] expectedMessageBuffers,
        int expectedCount)
    {
        var reader = new ChunkReader(new MemoryStream(input));

        var targetStream = new MemoryStream();
        var count = await reader.ReadMessageChunksToBufferStreamAsync(targetStream);
        var messageBuffers = targetStream.ToArray();

        count.Should().Be(expectedCount);
        messageBuffers.Should().Equal(expectedMessageBuffers);
    }

    [Fact]
    public async void ShouldResetBufferStreamPosition()
    {
        var data = GenerateMessages(1000, 128 * 1024);

        var reader = new ChunkReader(new MemoryStream(data.ToArray()));

        var bufferStream = new MemoryStream();
        bufferStream.Write(GenerateMessageChunk(1035));

        var bufferPosition = bufferStream.Position;

        var count = await reader.ReadMessageChunksToBufferStreamAsync(bufferStream);

        bufferStream.Position.Should().Be(bufferPosition);
    }

    [Fact]
    public async void ShouldThrowCancellationWhenReadAsyncIsCancelled()
    {
        var testStream = AsyncTestStream.CreateCancellingStream();
        testStream.SetLength(4);
        testStream.Position = 0;
        var reader = new ChunkReader(testStream);

        var ex = await Record.ExceptionAsync(() => reader.ReadMessageChunksToBufferStreamAsync(new MemoryStream()));

        ex.Should().NotBeNull();
        ex.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async void ShouldThrowIOExceptionWhenReadAsyncIsFaultedSynchronously()
    {
        var testStream = AsyncTestStream.CreateSyncFailingStream(new IOException("some error"));
        testStream.SetLength(4);
        testStream.Position = 0;
        var reader = new ChunkReader(testStream);

        var ex = await Record.ExceptionAsync(() => reader.ReadMessageChunksToBufferStreamAsync(new MemoryStream()));

        ex.Should().NotBeNull();
        ex.Should().BeAssignableTo<IOException>().Which.Message.Should().Be("some error");
    }

    [Fact]
    public async void ShouldThrowIOExceptionWhenReadAsyncIsFaulted()
    {
        var testStream = AsyncTestStream.CreateFailingStream(new IOException("some error"));
        testStream.SetLength(4);
        testStream.Position = 0;
        var reader = new ChunkReader(testStream);

        var ex = await Record.ExceptionAsync(() => reader.ReadMessageChunksToBufferStreamAsync(new MemoryStream()));

        ex.Should().NotBeNull();
        ex.Should().BeAssignableTo<IOException>().Which.Message.Should().Be("some error");
    }

    [Fact]
    public async void ShouldResetBufferStreamPositionAsync()
    {
        var data = GenerateMessages(1000, 128 * 1024);

        var reader = new ChunkReader(new MemoryStream(data.ToArray()));

        var bufferStream = new MemoryStream();
        bufferStream.Write(GenerateMessageChunk(1035));

        var bufferPosition = bufferStream.Position;

        var count = await reader.ReadMessageChunksToBufferStreamAsync(bufferStream);

        bufferStream.Position.Should().Be(bufferPosition);
    }

    [Fact]
    public async void ShouldReadSingleMessageStreamLargerThanBufferSizeAsync()
    {
        const int chunkSize = 22 * 1024;
        const int totalStreamSizeByte = 2 * chunkSize;
        var inputStream =
            new MemoryStream(
                GenerateMessages(
                    chunkSize,
                    totalStreamSizeByte)); // Will create a message of two 11k chunks, these will straddle the 16k internal buffer size...

        var resultStream = new MemoryStream();
        var reader = new ChunkReader(inputStream);

        var count = await reader.ReadMessageChunksToBufferStreamAsync(resultStream);

        count.Should().Be(1);
    }

    [Fact]
    public async void ShouldReadMultipleMessageStreamLargerThanBufferSizeAsync()
    {
        const int chunkSize = 11 * 1024;
        const int totalStreamSizeByte = 2 * chunkSize;
        var inputStream = new MemoryStream();
        inputStream.Write(GenerateMessages(chunkSize, totalStreamSizeByte)); //Add a message made of two 11k chunks.
        inputStream.Write(
            GenerateMessages(chunkSize, totalStreamSizeByte)); //Add another message made of two 11k chunks.

        inputStream.Position = 0;

        var resultStream = new MemoryStream();
        var reader = new ChunkReader(inputStream);

        var count = await reader.ReadMessageChunksToBufferStreamAsync(resultStream);

        count.Should().Be(2);
    }

    private static byte[] GenerateMessageChunk(int messageSize)
    {
        var buffer = Enumerable.Range(0, messageSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
        var stream = new MemoryStream();
        var cs = new DriverContext(
            new Uri("bolt://localhost:7687"),
            new StaticAuthTokenManager(AuthTokens.None),
            new Config());

        var writer = new ChunkWriter(stream, cs, new Mock<ILogger>().Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        // Append end of message marker
        writer.OpenChunk();
        writer.CloseChunk();

        writer.Flush();
        writer.SendAsync().GetAwaiter().GetResult();

        return stream.ToArray();
    }

    private static byte[] GenerateMessages(int messageSizePerChunk, int maxBytes)
    {
        var message = GenerateMessageChunk(messageSizePerChunk);
        var stream = new MemoryStream();

        while (true)
        {
            if (stream.Length + message.Length > maxBytes)
            {
                break;
            }

            stream.Write(message, 0, message.Length);
        }

        return stream.ToArray();
    }
}
