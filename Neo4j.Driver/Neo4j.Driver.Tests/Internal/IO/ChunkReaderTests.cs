// Copyright (c) 2002-2020 "Neo4j,"
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
using Neo4j.Driver.Internal.IO.Utils;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Internal.IO
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
            var ex = Record.Exception(() => new ChunkReader(null, Mock.Of<ILogger>()));

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
        [InlineData(new byte[] { 0x00, 0x00 }, 
                                 new byte[] { }, 0)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 
                                 0x00, 0x02, 0x01, 0x02, 
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 
                                 0x00, 0x01, 0x01, 
                                 0x00, 0x01, 0x02, 
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00, 
                                 0x00, 0x01, 0x01, 
                                 0x00, 0x01, 0x02, 
                                 0x00, 0x00, 
                                 0x00, 0x03, 0x00, 0x01, 0x02, 
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 }, 2)]
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
        [InlineData(new byte[] { 0x00, 0x00 }, 
                                 new byte[] { }, 0)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00,      
                                 0x00, 0x02, 0x01, 0x02,     
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00,      
                                 0x00, 0x01, 0x01,           
                                 0x00, 0x01, 0x02,       
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02 }, 1)]
        [InlineData(new byte[] { 0x00, 0x01, 0x00,      
                                 0x00, 0x01, 0x01,           
                                 0x00, 0x01, 0x02,       
                                 0x00, 0x00,         
                                 0x00, 0x03, 0x00, 0x01, 0x02,       
                                 0x00, 0x00 }, 
                                 new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 }, 2)]
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
        public void ShouldHandleEmptyStreamGracefully(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));
            var targetStream = new MemoryStream();
            int numMessages = 0;
            var ex = Record.Exception(() => numMessages = reader.ReadNextMessages(targetStream));

            ex.Should().BeNull();
            numMessages.Should().Be(0);           
        }

        [Theory]
        [InlineData(new byte[] { })]
        public async void ShouldHandleEmptyStreamGracefullyAsync(byte[] input)
        {
            var reader = new ChunkReader(new MemoryStream(input));
            var targetStream = new MemoryStream();
            int numMessages = 0;
            var ex = await Record.ExceptionAsync(async () => numMessages = await reader.ReadNextMessagesAsync(targetStream));

            ex.Should().BeNull();
            numMessages.Should().Be(0);
        }

        [Theory]
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
        [InlineData(new byte[] { 0x00, 0x00,                    //NOOP
                                 0x00, 0x01, 0x00,              //Message multichunk
                                 0x00, 0x01, 0x01,
                                 0x00, 0x01, 0x02,
                                 0x00, 0x00,                    //End of message
                                 0x00, 0x00,                    //NOOP
                                 0x00, 0x00,                    //NOOP
                                 0x00, 0x03, 0x00, 0x01, 0x02,  //Message single chunk
                                 0x00, 0x00,                    //End of message
                                 0x00, 0x00, },                 //NOOP
                                 new byte[] { 0x00, 0x01, 0x02, 0x00, 0x01, 0x02 }, 2)]
        public void ShouldReadNoopsBetweenMessages(byte[] input, byte[] expectedMessageBuffers, int expectedCount)
        {
            var reader = new ChunkReader(new MemoryStream(input));

            var targetStream = new MemoryStream();
            var count = reader.ReadNextMessages(targetStream);
            var messageBuffers = targetStream.ToArray();

            count.Should().Be(expectedCount);
            messageBuffers.Should().Equal(expectedMessageBuffers);
        }

        [Fact]
        public void ShouldResetBufferStreamPosition()
        {
            var data = GenerateMessages(1000, 128 * 1024);

            var logger = new Mock<ILogger>();
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
            var testStream = AsyncTestStream.CreateCancellingStream();
            testStream.SetLength(4);
            testStream.Position = 0;
            var reader = new ChunkReader(testStream);

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

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

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

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

            var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

            ex.Should().NotBeNull();
            ex.Should().BeAssignableTo<IOException>().Which.Message.Should().Be("some error");
        }

        [Fact]
        public async void ShouldResetBufferStreamPositionAsync()
        {
            var data = GenerateMessages(1000, 128 * 1024);

            var logger = new Mock<ILogger>();
            var reader = new ChunkReader(new MemoryStream(data.ToArray()), logger.Object);

            var bufferStream = new MemoryStream();
            bufferStream.Write(GenerateMessageChunk(1035));

            var bufferPosition = bufferStream.Position;

            var count = await reader.ReadNextMessagesAsync(bufferStream);

            bufferStream.Position.Should().Be(bufferPosition);
        }


        [Fact]
        public async void ShouldReadSingleMessageStreamLargerThanBufferSizeAsync()
        {
            const int messageSizeByte = 22 * 1024;
            const int totalStreamSizeByte = (messageSizeByte + 4);
            var inputStream = new MemoryStream(GenerateMessages(messageSizeByte, totalStreamSizeByte));  // Will create a stream of two 11k messages, these will straddle the 16k internal buffer size...
            var resultStream = new MemoryStream();
            var reader = new ChunkReader(inputStream);
            
            var count = await reader.ReadNextMessagesAsync(resultStream);

            count.Should().Be(1);
        }

        [Fact]
        public void ShouldReadSingleMessageStreamLargerThanBufferSize()
        {
            const int messageSizeByte = 22 * 1024;
            const int totalStreamSizeByte = (messageSizeByte + 4);
            var inputStream = new MemoryStream(GenerateMessages(messageSizeByte, totalStreamSizeByte));  // Will create a stream of two 11k messages, these will straddle the 16k internal buffer size...
            var resultStream = new MemoryStream();
            var reader = new ChunkReader(inputStream);
            
            var count = reader.ReadNextMessages(resultStream);

            count.Should().Be(1);
        }

        [Fact]
        public async void ShouldReadMultipleMessageStreamLargerThanBufferSizeAsync()
        {
            const int messageSizeByte = 11 * 1024;
            const int totalStreamSizeByte = 2 * (messageSizeByte + 4);
            var inputStream = new MemoryStream(GenerateMessages(messageSizeByte, totalStreamSizeByte));  // Will create a stream of two 11k messages, these will straddle the 16k internal buffer size...
            var resultStream = new MemoryStream();
            var reader = new ChunkReader(inputStream);
            
            var count = await reader.ReadNextMessagesAsync(resultStream);

            count.Should().Be(2);
        }

        [Fact]
        public void ShouldReadMultipleMessageStreamLargerThanBufferSize()
        {
            const int messageSizeByte = 11 * 1024;
            const int totalStreamSizeByte = 2 * (messageSizeByte + 4);
            var inputStream = new MemoryStream(GenerateMessages(messageSizeByte, totalStreamSizeByte));  // Will create a stream of two 11k messages, these will straddle the 16k internal buffer size...
            var resultStream = new MemoryStream();
            var reader = new ChunkReader(inputStream);
            
            var count = reader.ReadNextMessages(resultStream);

            count.Should().Be(2);
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
