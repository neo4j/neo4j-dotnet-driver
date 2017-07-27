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

                reader.ReadNextChunk(targetStream);

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

                await reader.ReadNextChunkAsync(targetStream);

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

                var ex = await Record.ExceptionAsync(() => reader.ReadNextChunkAsync(new MemoryStream()));

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

                reader.ReadNextChunk(targetStream);

                var real = targetStream.ToArray();

                real.Should().NotBeNull();
                real.Should().HaveCount(ushort.MaxValue);
                real.Should().Contain(0);

                chunkBuffer.Length.Should().Be(2);
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

                await reader.ReadNextChunkAsync(targetStream);

                var real = targetStream.ToArray();

                real.Should().NotBeNull();
                real.Should().HaveCount(ushort.MaxValue);
                real.Should().Contain(0);

                chunkBuffer.Length.Should().Be(2);
            }

        }

        //public class ReadBSyteMethod
        //{
        //    [Theory]
        //    [InlineData(new byte[] {0x00, 0x01, 0x80, 0x00, 0x00}, sbyte.MinValue)]
        //    [InlineData(new byte[] {0x00, 0x01, 0x7F, 0x00, 0x00}, sbyte.MaxValue)]
        //    [InlineData(new byte[] {0x00, 0x01, 0x00, 0x00, 0x00}, 0)]
        //    [InlineData(new byte[] {0x00, 0x01, 0xFF, 0x00, 0x00}, -1)]
        //    public void ShouldReturnTheCorrectValue(byte[] response, sbyte correctValue)
        //    {
        //        var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(response);
        //        var actual = chunkedInput.NextSByte();
        //        actual.Should().Be(correctValue); //, $"Got: {actual}, expected: {correctValue}");
        //    }
        //}

        //public class MultipleChunksTests
        //{
        //    private readonly ITestOutputHelper _output;

        //    public MultipleChunksTests(ITestOutputHelper output)
        //    {
        //        _output = output;
        //    }

        //    [Theory]
        //    //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
        //    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02})]
        //    public void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
        //    {
        //        var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);

        //        byte[] actual = chunkedInput.ReadBytes(3);

        //        actual.Should().Equal(correctValue);
        //    }

        //    [Theory]
        //    //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
        //    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
        //    public void ShouldLogBytes(byte[] input, byte[] correctValue)
        //    {
        //        var loggerMock = new Mock<ILogger>();
        //        loggerMock.Setup(x => x.Trace(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<int>(), It.IsAny<int>()))
        //            .Callback<string, object[]>((s, o) => _output.WriteLine(s + ((byte[])o[0]).ToHexString(showX: true)));

        //        var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input, loggerMock.Object);

        //        byte[] actual = chunkedInput.ReadBytes(3);
        //        actual.Should().Equal(correctValue);
        //        loggerMock.Verify(x => x.Trace("S: ", It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
        //    }

        //    [Theory]
        //    //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
        //    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
        //    public void ShouldReadMessageBiggerThanChunkSize(byte[] input, byte[] correctValue)
        //    {
        //        var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);

        //        byte[] actual = chunkedInput.ReadBytes(3);
        //        actual.Should().Equal(correctValue);
        //    }
        //}

        //public class ChunkHeaderTests
        //{
        //    private readonly Random _random = new Random();
        //    public byte Getbyte()
        //    {
        //        var num = _random.Next(0, 26); // 0 to 25
        //        byte letter = (byte)('a' + num);
        //        return letter;
        //    }

        //    [Fact]
        //    public void ShouldReadHeaderWithinUnsignedShortRange()
        //    {
        //        for (var i = 1; i <= UInt16.MaxValue; i = (i << 1) + 1) // i: [0x1, 0xFFFF]
        //        {
        //            ushort chunkHeaderSize = (ushort)(i & 0xFFFF);

        //            var input = new byte[chunkHeaderSize + 2 + 2]; // 0xXX, 0xXX, ..., 0x00, 0x00
        //            input[0] = (byte)((chunkHeaderSize & 0xFF00) >> 8);
        //            input[1] = (byte)(chunkHeaderSize & 0xFF);
        //            for (int j = 2; j < chunkHeaderSize + 2; j++)
        //            {
        //                input[j] = Getbyte();
        //            }

        //            var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);
        //            byte[] actual = chunkedInput.ReadBytes(chunkHeaderSize);
        //            for (int j = 0; j < actual.Length; j++)
        //            {
        //                actual[j].Should().Be(input[2 + j]);
        //            }
        //        }
        //    }
        //}

    }
}
