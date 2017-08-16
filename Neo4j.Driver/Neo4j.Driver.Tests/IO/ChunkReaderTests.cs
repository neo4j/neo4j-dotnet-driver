// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Threading;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.Tests.TcpSocketClientTestSetup;

namespace Neo4j.Driver.Tests.IO
{
    public class ChunkReaderTests
    {

        public class Constructor
        {

            [Fact]
            public void ShouldThrowArgumentNullOnNullStream()
            {
                var ex = Record.Exception(() => new ChunkReader(null));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullOnNullStreamWithLogger()
            {
                var mockLogger = new Mock<ILogger>();

                var ex = Record.Exception(() => new ChunkReader(null, mockLogger.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentOutOfRangeOnNotReadableStream()
            {
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(false);

                var ex = Record.Exception(() => new ChunkReader(mockStream.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldThrowArgumentOutOfRangeOnNotReadableStreamWithLogger()
            {
                var mockLogger = new Mock<ILogger>();
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(false);

                var ex = Record.Exception(() => new ChunkReader(mockStream.Object, mockLogger.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldAcceptNullLogger()
            {
                var ex = Record.Exception(() => new ChunkReader(new MemoryStream(), null));

                ex.Should().BeNull();
            }

        }

        public class ReadNextChunkMethod
        {

            [Theory]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var reader = new ChunkReader(new MemoryStream(input));
                var targetStream = new MemoryStream();

                reader.ReadNextMessage(targetStream);

                var real = targetStream.ToArray();

                real.Should().Equal(correctValue);
            }

        }

        public class ReadNextChunkAsyncMethod
        {

            [Theory]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public async void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var reader = new ChunkReader(new MemoryStream(input));
                var targetStream = new MemoryStream();

                await reader.ReadNextMessageAsync(targetStream);

                var real = targetStream.ToArray();

                real.Should().Equal(correctValue);
            }

            [Fact]
            public async void ShouldThrowWhenInnerTaskThrowsException()
            {
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(true);
                mockStream.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Throws<InvalidOperationException>();
                var reader = new ChunkReader(mockStream.Object);

                var ex = await Record.ExceptionAsync(() => reader.ReadNextMessageAsync(new MemoryStream()));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<InvalidOperationException>();
            }

        }

        public class Cleanup
        {


            [Fact]
            public void ShouldCleanupChunkBufferIfItExceedsMaxChunkBufferSize()
            {
                byte[] maxSizeChunkBuffer = new byte[ushort.MaxValue + 2 + 2];
                byte[] chunkSizeBuffer = PackStreamBitConverter.GetBytes(ushort.MaxValue);
                for (var i = 0; i < chunkSizeBuffer.Length; i++) maxSizeChunkBuffer[i] = chunkSizeBuffer[i];
                var chunkBuffer = new MemoryStream();
                var targetStream = new MemoryStream();
                var reader = new ChunkReader(new MemoryStream(maxSizeChunkBuffer), chunkBuffer, null);

                reader.ReadNextMessage(targetStream);

                var real = targetStream.ToArray();

                real.Should().NotBeNull();
                real.Should().HaveCount(ushort.MaxValue);
                real.Should().Contain(0);

                chunkBuffer.Length.Should().Be(0);
                chunkBuffer.Position.Should().Be(0);
            }

            [Fact]
            public async void ShouldCleanupChunkBufferIfItExceedsMaxChunkBufferSizeAsync()
            {
                byte[] maxSizeChunkBuffer = new byte[ushort.MaxValue + 2 + 2];
                byte[] chunkSizeBuffer = PackStreamBitConverter.GetBytes(ushort.MaxValue);
                for (var i = 0; i < chunkSizeBuffer.Length; i++) maxSizeChunkBuffer[i] = chunkSizeBuffer[i];
                var chunkBuffer = new MemoryStream();
                var targetStream = new MemoryStream();
                var reader = new ChunkReader(new MemoryStream(maxSizeChunkBuffer), chunkBuffer, null);

                await reader.ReadNextMessageAsync(targetStream);

                var real = targetStream.ToArray();

                real.Should().NotBeNull();
                real.Should().HaveCount(ushort.MaxValue);
                real.Should().Contain(0);

                chunkBuffer.Length.Should().Be(0);
                chunkBuffer.Position.Should().Be(0);
            }

        }
        
    }
}
