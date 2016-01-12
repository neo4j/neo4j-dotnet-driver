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
using Moq;
using Neo4j.Driver.Internal.messaging;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PackStreamV1PackerTests
    {
        [Fact]
        public void PacksInitMessageCorrectly()
        {
            var mockTcpSocketClient = new Mock<ITcpSocketClient>();
            var mockStream = new Mock<Stream>();
            var receieved = string.Empty;

            mockStream
                .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                .Callback<byte[], int, int>((buffer, start, size) => receieved = $"{buffer.ToHexString(start, size)}");

            mockTcpSocketClient
                .Setup(t => t.WriteStream)
                .Returns(mockStream.Object);

            var packer = new PackStreamV1Packer(mockTcpSocketClient.Object, new BigEndianTargetBitConverter());
            packer.HandleInitMessage(new InitMessage("a"));
            packer.Flush();

            byte[] expectedBytes =
                new byte[] {0x00, 0x04, 0xB1, 0x01, 0x81, 0x61, 0x00, 0x00}.PadRight(PackStreamV1Chunker.BufferSize);
            mockStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                $"Recieved {receieved}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, 8)}");
        }
    }

    public static class ByteExtensions
    {
        public static byte[] PadRight(this byte[] bytes, int totalSize)
        {
            var output = new byte[totalSize];
            Array.Copy(bytes, output, bytes.Length);
            return output;
        }

        public static string ToHexString(this byte[] bytes, int start, int size)
        {
            if (bytes == null)
                return "NULL";

            var destination = new byte[size];
            Array.Copy(bytes, start, destination, 0, size);

            return BitConverter.ToString(destination).Replace("-", " ");
        }
    }
}