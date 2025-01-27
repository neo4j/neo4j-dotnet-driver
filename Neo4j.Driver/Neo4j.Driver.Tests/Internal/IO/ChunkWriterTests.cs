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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.IO;

public class ChunkWriterTests
{
    private readonly Mock<ILogger> _logger = new();

    private static DriverContext TestContext(int defaultRead, int defaultWrite, int maxRead, int maxWrite)
    {
        return TestDriverContext.With(
            config: x =>
                x.WithDefaultReadBufferSize(defaultRead)
                    .WithDefaultWriteBufferSize(defaultWrite)
                    .WithMaxReadBufferSize(maxRead)
                    .WithMaxWriteBufferSize(maxWrite));
    }

    [Fact]
    public void ShouldThrowWhenConstructedUsingUnreadableStream()
    {
        var mockStream = new Mock<Stream>();
        mockStream.Setup(l => l.CanWrite).Returns(false);

        var ex = Record.Exception(
            () =>
                new ChunkWriter(mockStream.Object, TestDriverContext.MockContext, _logger.Object));

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ShouldWriteToUnderlyingStreamUponSend()
    {
        var buffer = new byte[1024];
        var stream = new MemoryStream();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, _logger.Object);

        // Write data
        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        stream.Length.Should().Be(0);

        await writer.SendAsync();

        stream.Length.Should().Be(buffer.Length + 2);
    }

    [Fact]
    public async Task ShouldWriteToUnderlyingStreamUponSendAsync()
    {
        var buffer = new byte[1024];
        var stream = new MemoryStream();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, _logger.Object);

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
    public async Task ShouldCloseTheChunkWithCorrectSize(int chunkSize)
    {
        var buffer = Enumerable.Range(0, chunkSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
        var stream = new MemoryStream();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, _logger.Object);

        // Write data
        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        // End Of Message Marker
        writer.OpenChunk();
        writer.CloseChunk();

        // Write To Underlying Stream
        await writer.SendAsync();

        var constructed = await ConstructMessage(stream.ToArray());

        constructed.Should().HaveCount(chunkSize);
        constructed.Should().Equal(buffer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(Constants.ChunkBufferSize)]
    [InlineData(Constants.MaxChunkSize)]
    [InlineData(Constants.MaxChunkSize * 3)]
    public async Task ShouldCloseTheChunkWithCorrectSizeAsync(int chunkSize)
    {
        var buffer = Enumerable.Range(0, chunkSize).Select(i => i % byte.MaxValue).Select(i => (byte)i).ToArray();
        var stream = new MemoryStream();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, _logger.Object);

        // Write data
        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        // End Of Message Marker
        writer.OpenChunk();
        writer.CloseChunk();

        // Write To Underlying Stream
        await writer.SendAsync();

        var constructed = await ConstructMessage(stream.ToArray());

        constructed.Should().HaveCount(chunkSize);
        constructed.Should().Equal(buffer);
    }

    [Fact]
    public async Task ShouldLogDataOnSend()
    {
        var buffer = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
        var stream = new MemoryStream();
        var logger = LoggingHelper.GetTraceEnabledLogger();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, logger.Object);

        // Write data
        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Never);

        await writer.SendAsync();

        logger.Verify(x => x.Trace("C: {0}", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShouldLogDataOnSendAsync()
    {
        var buffer = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();
        var stream = new MemoryStream();
        var logger = LoggingHelper.GetTraceEnabledLogger();
        var writer = new ChunkWriter(stream, TestDriverContext.MockContext, logger.Object);

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
    public async Task ShouldNotResetCapacityWhenBelowMaxBufferSize(
        int defaultBufferSize,
        int maxBufferSize,
        int messageSize)
    {
        var buffer = new byte[messageSize];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var context = TestContext(defaultBufferSize, defaultBufferSize, maxBufferSize, maxBufferSize);
        var writer = new ChunkWriter(stream, context, logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        logger.Verify(
            l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()),
            Times.Never);
    }

    [Theory]
    [InlineData(100, 200, 5)]
    [InlineData(100, 200, 10)]
    [InlineData(100, 200, 50)]
    [InlineData(100, 200, 98)]
    public async void ShouldNotResetCapacityWhenBelowMaxBufferSizeAsync(
        int defaultBufferSize,
        int maxBufferSize,
        int messageSize)
    {
        var buffer = new byte[messageSize];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var context = TestContext(defaultBufferSize, defaultBufferSize, maxBufferSize, maxBufferSize);
        var writer = new ChunkWriter(
            stream,
            context,
            logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        await writer.SendAsync();

        logger.Verify(
            l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()),
            Times.Never);
    }

    [Theory]
    [InlineData(100, 200, 100)]
    [InlineData(100, 200, 150)]
    [InlineData(100, 200, 200)]
    [InlineData(100, 200, 500)]
    public async Task ShouldResetCapacityWhenAboveMaxBufferSize(
        int defaultBufferSize,
        int maxBufferSize,
        int messageSize)
    {
        var buffer = new byte[messageSize];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var context = TestContext(defaultBufferSize, defaultBufferSize, maxBufferSize, maxBufferSize);
        var writer = new ChunkWriter(stream, context, logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Once);
    }

    [Theory]
    [InlineData(100, 200, 100)]
    [InlineData(100, 200, 150)]
    [InlineData(100, 200, 200)]
    [InlineData(100, 200, 500)]
    public async void ShouldResetCapacityWhenAboveMaxBufferSizeAsync(
        int defaultBufferSize,
        int maxBufferSize,
        int messageSize)
    {
        var buffer = new byte[messageSize];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var context = TestContext(defaultBufferSize, defaultBufferSize, maxBufferSize, maxBufferSize);

        var writer = new ChunkWriter(stream, context, logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();

        await writer.SendAsync();

        logger.Verify(l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task ShouldResetCapacityWhenAboveMaxBufferSizeAfterEachSend()
    {
        var buffer = new byte[1536];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var settings = TestContext(512, 512, 1024, 1024);
        var writer = new ChunkWriter(stream, settings, logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        logger.Verify(
            l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ShouldResetCapacityWhenAboveMaxBufferSizeAfterEachSendAsync()
    {
        var buffer = new byte[1536];
        var stream = new MemoryStream();
        var logger = new Mock<ILogger>();
        var context = TestContext(512, 512, 1024, 1024);
        var writer = new ChunkWriter(stream, context, logger.Object);

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        writer.OpenChunk();
        writer.Write(buffer, 0, buffer.Length);
        writer.CloseChunk();
        await writer.SendAsync();

        logger.Verify(
            l => l.Info(It.IsRegex("^Shrinking write buffers to the"), It.IsAny<object[]>()),
            Times.Exactly(2));
    }

    private static async Task<byte[]> ConstructMessage(byte[] buffer)
    {
        var stream = new MemoryStream();
        var reader = new ChunkReader(new MemoryStream(buffer));

        await reader.ReadMessageChunksToBufferStreamAsync(stream);

        return stream.ToArray();
    }
}
