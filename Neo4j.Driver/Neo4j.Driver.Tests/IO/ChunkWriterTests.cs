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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class ChunkWriterTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldThrowExceptionIfChunkSizeLessThan6()
            {
                var ex = Xunit.Record.Exception(() => new ChunkWriter(new MemoryStream(), 5));
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldNotThrowExceptionIfChunkSizeIs6()
            {
                var ex = Xunit.Record.Exception(() => new ChunkWriter(new MemoryStream(), 6));
                ex.Should().BeNull();
            }

            [Fact]
            public void ShouldThrowExceptionIfChunkSizeGreaterThanSumOfUShortMaxAndChunkHeaderBufferSize()
            {
                var ex = Xunit.Record.Exception(() => new ChunkWriter(new MemoryStream(), ushort.MaxValue + 1));
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldNotThrowExceptionIfChunkSizeIsSumOfUShortMaxAndChunkHeaderBufferSize()
            {
                var ex = Xunit.Record.Exception(() => new ChunkWriter(new MemoryStream(), ushort.MaxValue));
                ex.Should().BeNull();
            }
        }

        public class WriteMethod
        {
            [Fact]
            public void ShouldWriteBytesCorrectlyWhenMessageIsGreaterThanChunkSize()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, 6);

                byte[] bytes = new byte[10];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i + 1);
                }

                chunker.WriteChunk(bytes, 0, bytes.Length);
                chunker.Flush();

                byte[] expected = {0x00, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x00, 0x04, 0x07, 0x08, 0x09, 0x0A};

                Assert.Equal(expected, stream.ToArray());
            }

            [Fact]
            public void ShouldBeAbleToWriteChunkWhoseSizeIsEqualToMaxU16Int()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, ushort.MaxValue);

                byte[] bytes = new byte[ushort.MaxValue];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i + 1);
                }

                chunker.WriteChunk(bytes, 0, bytes.Length);
                chunker.Flush();

                byte[] expected = new byte[ushort.MaxValue + 2];

                expected[0] = 0xFF;
                expected[1] = 0xFF;
                for (int i = 0; i < ushort.MaxValue; i++)
                {
                    expected[i + 2] = (byte)(i + 1);
                }

                Assert.Equal(expected, stream.ToArray());
            }

            [Fact]
            public void ShouldBeAbleToWriteZeroLengthChunk()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream);

                chunker.WriteChunk(new byte[0], 0, 0);
                chunker.Flush();

                Assert.Equal(new byte[2], stream.ToArray());
            }

            [Fact]
            public void ShouldLogOnFlush()
            {
                var loggerMock = new Mock<ILogger>();
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, loggerMock.Object);

                chunker.WriteChunk(new byte[0], 0, 0);
                chunker.Flush();

                loggerMock.Verify(x => x.Trace("C: ", new byte[2], 0, 2), Times.Once);
            }

            [Fact]
            public async void ShouldWriteBytesCorrectlyWhenMessageIsGreaterThanChunkSizeAsync()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, 6);

                byte[] bytes = new byte[10];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i + 1);
                }

                chunker.WriteChunk(bytes, 0, bytes.Length);
                await chunker.FlushAsync();

                byte[] expected = { 0x00, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x00, 0x04, 0x07, 0x08, 0x09, 0x0A };

                Assert.Equal(expected, stream.ToArray());
            }

            [Fact]
            public async void ShouldBeAbleToWriteChunkWhoseSizeIsEqualToMaxU16IntAsync()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, ushort.MaxValue);

                byte[] bytes = new byte[ushort.MaxValue];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i + 1);
                }

                chunker.WriteChunk(bytes, 0, bytes.Length);
                await chunker.FlushAsync();

                byte[] expected = new byte[ushort.MaxValue + 2];

                expected[0] = 0xFF;
                expected[1] = 0xFF;
                for (int i = 0; i < ushort.MaxValue; i++)
                {
                    expected[i + 2] = (byte)(i + 1);
                }

                Assert.Equal(expected, stream.ToArray());
            }

            [Fact]
            public async void ShouldBeAbleToWriteZeroLengthChunkAsync()
            {
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream);

                chunker.WriteChunk(new byte[0], 0, 0);
                await chunker.FlushAsync();

                Assert.Equal(new byte[2], stream.ToArray());
            }

            [Fact]
            public async void ShouldLogOnFlushAsync()
            {
                var loggerMock = new Mock<ILogger>();
                var stream = new MemoryStream();
                var chunker = new ChunkWriter(stream, loggerMock.Object);

                chunker.WriteChunk(new byte[0], 0, 0);
                await chunker.FlushAsync();

                loggerMock.Verify(x => x.Trace("C: ", new byte[2], 0, 2), Times.Once);
            }


        }

    }
}
