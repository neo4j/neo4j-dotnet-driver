// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.IO.Utils;
using Neo4j.Driver.Tests.TestUtil;
using Neo4j.Driver;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
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
            var ex = Record.Exception(() => new ChunkReader(null, Mock.Of<IDriverLogger>()));

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
        [InlineData(new byte[] { 0x00, 0x00 }, new byte[] { }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 }, 2)]
        public void ShouldReadMessageSpanningMultipleChunks(byte[] input, byte[] expectedMessageBuffers, int expectedCount)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var count = reader.ReadNextMessages(targetStream);
            var messageBuffers = targetStream.ToArray();

            count.Should().Be(expectedCount);
            messageBuffers.Should().Equal(expectedMessageBuffers);
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00 }, new byte[] { }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 }, 2)]
        public async void ShouldReadMessageSpanningMultipleChunksAsync(byte[] input, byte[] expectedMessageBuffers, int expectedCount)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var count = await reader.ReadNextMessagesAsync(targetStream);
            var messageBuffers = targetStream.ToArray();

            count.Should().Be(expectedCount);
            messageBuffers.Should().Equal(expectedMessageBuffers);
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x00, 0x01 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01 })]
        public void ShouldThrowWhenEndOfStreamIsDetected(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var ex = Record.Exception(() => reader.ReadNextMessages(targetStream));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<IOException>().Which.Message.Should().StartWith("Unexpected end of stream");
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x00, 0x01 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01 })]
        public async void ShouldThrowWhenEndOfStreamIsDetectedAsync(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(targetStream));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<IOException>().Which.Message.Should().StartWith("Unexpected end of stream");
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x01, 0x00 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02 })]
        public void ShouldThrowWhenEndOfMessageMarkerNotPresent(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var ex = Record.Exception(() => reader.ReadNextMessages(targetStream));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<IOException>().Which.Message.Should().StartWith("Unexpected end of stream");
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x01, 0x00 })]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02 })]
        public async void ShouldThrowWhenEndOfMessageMarkerNotPresentAsync(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(targetStream));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<IOException>().Which.Message.Should().StartWith("Unexpected end of stream");
        }

        [Fact]
        public void ShouldNotResetInternalBufferPositionsWhenWritableBufferIsLargerThanSetWatermark()
        {
            var input = GenerateMessageChunk(Constants.ChunkBufferSize -
                                             (Constants.ChunkBufferResetPositionsWatermark + 10));
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = reader.ReadNextMessages(new MemoryStream());

            count.Should().Be(1);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public async void ShouldNotResetInternalBufferPositionsWhenWritableBufferIsLargerThanSetWatermarkAsync()
        {
            var input = GenerateMessageChunk(Constants.ChunkBufferSize -
                                             (Constants.ChunkBufferResetPositionsWatermark + 10));
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = await reader.ReadNextMessagesAsync(new MemoryStream());

            count.Should().Be(1);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public void ShouldResetInternalBufferPositionsWhenWritableBufferIsSmallerThanSetWatermark()
        {
            var input = GenerateMessageChunk(Constants.ChunkBufferSize - Constants.ChunkBufferResetPositionsWatermark);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = reader.ReadNextMessages(new MemoryStream());

            count.Should().Be(1);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async void ShouldResetInternalBufferPositionsWhenWritableBufferIsSmallerThanSetWatermarkAsync()
        {
            var input = GenerateMessageChunk(Constants.ChunkBufferSize - Constants.ChunkBufferResetPositionsWatermark);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = await reader.ReadNextMessagesAsync(new MemoryStream());

            count.Should().Be(1);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void ShouldNotResetInternalBufferPositionsWhenWritableBufferIsLargerThanSetWatermarkWithConsecutiveMessages()
        {
            const int messageSizePerChunk = 1000;
            const int maxBytes = Constants.ChunkBufferSize - (Constants.ChunkBufferResetPositionsWatermark + 10);

            var input = GenerateMessages(messageSizePerChunk, maxBytes);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = reader.ReadNextMessages(new MemoryStream());

            count.Should().BeGreaterOrEqualTo(maxBytes / messageSizePerChunk);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public async void ShouldNotResetInternalBufferPositionsWhenWritableBufferIsLargerThanSetWatermarkWithConsecutiveMessagesAsync()
        {
            const int messageSizePerChunk = 1000;
            const int maxBytes = Constants.ChunkBufferSize - (Constants.ChunkBufferResetPositionsWatermark + 10);

            var input = GenerateMessages(messageSizePerChunk, maxBytes);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = await reader.ReadNextMessagesAsync(new MemoryStream());

            count.Should().BeGreaterOrEqualTo(maxBytes / messageSizePerChunk);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public void ShouldResetInternalBufferPositionsWhenWritableBufferIsSmallerThanSetWatermarkWithConsecutiveMessages()
        {
            const int messageSizePerChunk = 1000;
            const int maxBytes = Constants.ChunkBufferSize;

            var input = GenerateMessages(messageSizePerChunk, maxBytes);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = reader.ReadNextMessages(new MemoryStream());

            count.Should().BeGreaterOrEqualTo(maxBytes / messageSizePerChunk);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async void ShouldResetInternalBufferPositionsWhenWritableBufferIsSmallerThanSetWatermarkWithConsecutiveMessagesAsync()
        {
            const int messageSizePerChunk = 1000;
            const int maxBytes = Constants.ChunkBufferSize;

            var input = GenerateMessages(messageSizePerChunk, maxBytes);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = await reader.ReadNextMessagesAsync(new MemoryStream());

            count.Should().BeGreaterOrEqualTo(maxBytes / messageSizePerChunk);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void ShouldResetInternalBufferPositionsWhenAMessageOfNChunks()
        {
            var size = 3 * Constants.MaxChunkSize;
            var input = GenerateMessageChunk(3 * Constants.MaxChunkSize);
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var reader = new ChunkReader(new MemoryStream(input), logger.Object);

            var count = reader.ReadNextMessages(new MemoryStream());

            count.Should().Be(1);
            logger.Verify(l => l.Trace(It.IsRegex(CompactingArgumentRegEx), It.IsAny<object[]>()), Times.AtLeast(size / Constants.ChunkBufferSize));
        }
        
        [Fact]
        public void ShouldResetBufferStreamPosition()
        {
            var data = GenerateMessages(1000, 128 * 1024);

            var logger = new Mock<IDriverLogger>();
            var reader = new ChunkReader(new MemoryStream(data.ToArray()), logger.Object);

            var bufferStream = new MemoryStream();
            bufferStream.Write(GenerateMessageChunk(1035));

            var bufferPosition = bufferStream.Position;

            var count = reader.ReadNextMessages(bufferStream);

            bufferStream.Position.Should().Be(bufferPosition);
        }

        [Fact]
        public async void ShouldThrowCancellationWhenReadAsyncIsCancelled()
        {
            var reader = new ChunkReader(AsyncTestStream.CreateCancellingStream());

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<OperationCanceledException>();
        }

        [Fact]
        public async void ShouldThrowIOExceptionWhenReadAsyncIsFaultedSynchronously()
        {
            var reader = new ChunkReader(AsyncTestStream.CreateSyncFailingStream(new IOException("some error")));

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<IOException>().Which.Message.Should().Be("some error");
        }

        [Fact]
        public async void ShouldThrowIOExceptionWhenReadAsyncIsFaulted()
        {
            var reader = new ChunkReader(AsyncTestStream.CreateFailingStream(new IOException("some error")));

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<IOException>().Which.Message.Should().Be("some error");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async void ShouldThrowIOExceptionWhenReadAsyncReturnsZeroOrLess(int returnValue)
        {
            var reader = new ChunkReader(AsyncTestStream.CreateStream(Task.FromResult(returnValue)));

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<IOException>().Which.Message.Should().StartWith("Unexpected end of stream, read returned");
        }

        [Fact]
        public async void ShouldResetBufferStreamPositionAsync()
        {
            var data = GenerateMessages(1000, 128 * 1024);

            var logger = new Mock<IDriverLogger>();
            var reader = new ChunkReader(new MemoryStream(data.ToArray()), logger.Object);

            var bufferStream = new MemoryStream();
            bufferStream.Write(GenerateMessageChunk(1035));

            var bufferPosition = bufferStream.Position;

            var count = await reader.ReadNextMessagesAsync(bufferStream);

            bufferStream.Position.Should().Be(bufferPosition);
        }


        private static byte[] GenerateMessageChunk(int messageSize)
        {
            var buffer = Enumerable.Range(0, messageSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream();
            var writer = new ChunkWriter(stream);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            // Append end of message marker
            writer.OpenChunk();
            writer.CloseChunk();

            writer.Flush();
            writer.Send();

            return stream.ToArray();
        }

        private static byte[] GenerateMessages(int messageSizePerChunk, int maxBytes)
        {
            var message = GenerateMessageChunk(messageSizePerChunk);
            var stream = new MemoryStream();

            while (true)
            {
                if (stream.Length + message.Length > maxBytes)
                    break;

                stream.Write(message, 0, message.Length);
            }

            return stream.ToArray();
        }
    }
}
