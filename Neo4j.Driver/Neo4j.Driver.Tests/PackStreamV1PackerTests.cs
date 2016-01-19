//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.messaging;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PackStreamV1PackerTests
    {
        public class WriterV1
        {
            [Fact]
            public void PacksInitMessageCorrectly()
            {
                var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                var mockStream = new Mock<Stream>();
                var received = string.Empty;

                mockStream
                    .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                    .Callback<byte[], int, int>((buffer, start, size) => received = $"{buffer.ToHexString(start, size)}");

                mockTcpSocketClient
                    .Setup(t => t.WriteStream)
                    .Returns(mockStream.Object);

                var writer =
                    new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter()).Writer;
                writer.Write(new InitMessage("a"));
                writer.Flush();

                byte[] expectedBytes =
                    new byte[] {0x00, 0x04, 0xB1, 0x01, 0x81, 0x61, 0x00, 0x00}.PadRight(
                        PackStreamV1ChunkedOutput.BufferSize);
                mockStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                    $"Received {received}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, 8)}");
            }
        }

        public class ReaderV1Tests
        {
            public class ReadMethod
            {
                [Fact]
                //todo - verify properly.
                public void UnpacksStructHeaderCorrectly()
                {
                    var mockTcpSocketClient = new Mock<ITcpSocketClient>();

                    var bytes = "00 03 b1 70 a0 00 00".ToByteArray();
                    TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);//new byte[]{ 0x00, 0x03, 0xb1, 0x70,0xa0, 0x00, 0x00 });

                    var reader = 
                        new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter()).Reader;
                    reader.Read(new Mock<IMessageResponseHandler>().Object);
                    mockTcpSocketClient.Object.ReadStream.Position.Should().Be(7);
                }

                [Theory]
                [InlineData(2147483648, new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 })]
                [InlineData(9223372036854775807, new byte[] { 0xCB, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
                public void UnpackLongCorrectly(long expected, byte[] data)
                {
                    UnpackNumCorrectly(expected, data);
                }

                [Theory]
                [InlineData(32768, new byte[] { 0xCA, 0x00, 0x00, 0x80, 0x00 })]
                public void UnpackIntCorrectly(int expected, byte[] data)
                {
                    UnpackNumCorrectly(expected, data);
                }

                [Theory]
                [InlineData(-16, new byte[] { 0xF0 })]
                [InlineData(42, new byte[] { 0x2A })]
                [InlineData(127, new byte[] { 0x7F })]
                public void UnpackTinyIntCorrectly(sbyte value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                [Theory]
                [InlineData(-128, new byte[] { 0xC8, 0x80 })]
                [InlineData(-17, new byte[] { 0xC8, 0xEF })]
                public void UnpackInt8Correctly(sbyte value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                [Theory]
                [InlineData(128, new byte[] { 0xC9, 0x00, 0x80 })]
                public void UnpackShortCorrectly(short value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                private void UnpackNumCorrectly(dynamic expected, byte[] data)
                {
                    var mockTcpSocketClient = new Mock<ITcpSocketClient>();

                    List<byte> bytes = new List<byte>
                    {
                        0x00, (byte)data.Length
                    };
                    bytes.AddRange(data);

                    TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes.ToArray());

                    PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                        new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter()).Reader;
                    var real = reader.UnpackValue();
                    Assert.Equal(expected, real);
                }
            } 
        }
    }
}