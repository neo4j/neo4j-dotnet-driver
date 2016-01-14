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

        private static void SetupResponse(Mock<ITcpSocketClient> mock, byte[] response )
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(response );
            memoryStream.Flush();
            memoryStream.Position = 0;
            mock.Setup(c => c.ReadStream).Returns(memoryStream);
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

                    var bytes = TestHelper.StringToByteArray("00 03 b1 70 a0 00 00");
                    SetupResponse(mockTcpSocketClient, bytes);//new byte[]{ 0x00, 0x03, 0xb1, 0x70,0xa0, 0x00, 0x00 });

                    var reader = 
                        new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter()).Reader;
                    reader.Read(new Mock<IMessageResponseHandler>().Object);
                    mockTcpSocketClient.Object.ReadStream.Position.Should().Be(7);
                }
            } 
        }
    }
}