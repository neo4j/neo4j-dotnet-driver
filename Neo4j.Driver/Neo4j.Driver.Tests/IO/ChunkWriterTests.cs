// Copyright (c) 2002-2018 "Neo4j,"
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
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.TestUtil;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class ChunkWriterTests
    {

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(Constants.MaxChunkSize + 1)]
        public void ShouldThrowWhenConstructedWithInvalidChunkSize(int chunkSize)
        {
            var ex = Record.Exception(() => new ChunkWriter(new MemoryStream(), chunkSize));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldThrowWhenConstructedUsingUnreadableStream()
        {
            var mockLogger = new Mock<Stream>();
            mockLogger.Setup(l => l.CanWrite).Returns(false);

            var ex = Record.Exception(() => new ChunkWriter(mockLogger.Object));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldWriteToUnderlyingStreamUponSend()
        {
            var buffer = new byte[1024];
            var stream = new MemoryStream();
            var writer = new ChunkWriter(stream);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            stream.Length.Should().Be(0);

            writer.Send();

            stream.Length.Should().Be(buffer.Length + 2);
        }

        [Fact]
        public async void ShouldWriteToUnderlyingStreamUponSendAsync()
        {
            var buffer = new byte[1024];
            var stream = new MemoryStream();
            var writer = new ChunkWriter(stream);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            stream.Length.Should().Be(0);

            await writer.SendAsync();

            stream.Length.Should().Be(buffer.Length + 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(Constants.ChunkBufferSize)]
        [InlineData(Constants.MaxChunkSize)]
        [InlineData(Constants.MaxChunkSize * 3)]
        public void ShouldCloseTheChunkWithCorrectSize(int chunkSize)
        {
            var buffer = Enumerable.Range(0, chunkSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream();
            var writer = new ChunkWriter(stream);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            // End Of Message Marker
            writer.OpenChunk();
            writer.CloseChunk();

            // Write To Underlying Stream
            writer.Send();

            var constructed = ConstructMessage(stream.ToArray());

            constructed.Should().HaveCount(chunkSize);
            constructed.Should().Equal(buffer);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(Constants.ChunkBufferSize)]
        [InlineData(Constants.MaxChunkSize)]
        [InlineData(Constants.MaxChunkSize * 3)]
        public async void ShouldCloseTheChunkWithCorrectSizeAsync(int chunkSize)
        {
            var buffer = Enumerable.Range(0, chunkSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream();
            var writer = new ChunkWriter(stream);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            // End Of Message Marker
            writer.OpenChunk();
            writer.CloseChunk();

            // Write To Underlying Stream
            await writer.SendAsync();

            var constructed = ConstructMessage(stream.ToArray());

            constructed.Should().HaveCount(chunkSize);
            constructed.Should().Equal(buffer);
        }

        [Fact]
        public void ShouldLogDataOnSend()
        {
            var buffer = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream();
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var writer = new ChunkWriter(stream, logger.Object);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Never);

            writer.Send();

            logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async void ShouldLogDataOnSendAsync()
        {
            var buffer = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
            var stream = new MemoryStream();
            var logger = LoggingHelper.GetTraceEnabledLogger();
            var writer = new ChunkWriter(stream, logger.Object);

            // Write data
            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Never);

            await writer.SendAsync();

            logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData(100, 200, 5)]
        [InlineData(100, 200, 10)]
        [InlineData(100, 200, 50)]
        [InlineData(100, 200, 98)]
        public void ShouldNotResetCapacityWhenBelowMaxBufferSize(int defaultBufferSize, int maxBufferSize, int messageSize)
        {
            var buffer = new byte[messageSize];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            writer.Send();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Never);
        }

        [Theory]
        [InlineData(100, 200, 5)]
        [InlineData(100, 200, 10)]
        [InlineData(100, 200, 50)]
        [InlineData(100, 200, 98)]
        public async void ShouldNotResetCapacityWhenBelowMaxBufferSizeAsync(int defaultBufferSize, int maxBufferSize, int messageSize)
        {
            var buffer = new byte[messageSize];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            await writer.SendAsync();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Never);
        }

        [Theory]
        [InlineData(100, 200, 100)]
        [InlineData(100, 200, 150)]
        [InlineData(100, 200, 200)]
        [InlineData(100, 200, 500)]
        public void ShouldResetCapacityWhenAboveMaxBufferSize(int defaultBufferSize, int maxBufferSize, int messageSize)
        {
            var buffer = new byte[messageSize];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            writer.Send();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Once);
        }

        [Theory]
        [InlineData(100, 200, 100)]
        [InlineData(100, 200, 150)]
        [InlineData(100, 200, 200)]
        [InlineData(100, 200, 500)]
        public async void ShouldResetCapacityWhenAboveMaxBufferSizeAsync(int defaultBufferSize, int maxBufferSize, int messageSize)
        {
            var buffer = new byte[messageSize];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();

            await writer.SendAsync();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void ShouldResetCapacityWhenAboveMaxBufferSizeAfterEachSend()
        {
            var buffer = new byte[1536];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, 512, 1024, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            writer.Send();

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            writer.Send();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Exactly(2));
        }

        [Fact]
        public async void ShouldResetCapacityWhenAboveMaxBufferSizeAfterEachSendAsync()
        {
            var buffer = new byte[1536];
            var stream = new MemoryStream();
            var logger = new Mock<IDriverLogger>();
            var writer = new ChunkWriter(stream, 512, 1024, logger.Object);

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            await writer.SendAsync();

            writer.OpenChunk();
            writer.Write(buffer, 0, buffer.Length);
            writer.CloseChunk();
            await writer.SendAsync();

            logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Exactly(2));
        }

        private static byte[] ConstructMessage(byte[] buffer)
        {
            var stream = new MemoryStream();
            var reader = new ChunkReader(new MemoryStream(buffer));

            reader.ReadNextMessages(stream);

            return stream.ToArray();
        }

    }
}