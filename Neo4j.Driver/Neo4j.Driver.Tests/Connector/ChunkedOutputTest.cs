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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class ChunkedOutputTest
    {
        public class Constructor
        {
            [Fact]
            public void ShouldThrowExceptionIfChunkSizeLessThan8()
            {
                var ex = Xunit.Record.Exception(() => new ChunkedOutputStream(null, null, null, 7));
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldFlushBytesCorrectlyWhenMessageIsBiggerThanChunkSize()
            {
                var mockClient = new Mock<ITcpSocketClient>();
                var mockWriteStream = new Mock<Stream>();
                mockClient.Setup(x => x.WriteStream).Returns(mockWriteStream.Object);
                var mockLogger = new Mock<ILogger>();

                var chunker = new ChunkedOutputStream(mockClient.Object, new BigEndianTargetBitConverter(), mockLogger.Object, 8);

                byte[] bytes = new byte[10];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(i+1);
                }

                chunker.Write(bytes);
                chunker.Flush();

                byte[] expected1 = {0x00,0x04,0x01, 0x02, 0x03, 0x04, 0x00, 0x00};
                byte[] expected2 = { 0x00, 0x04, 0x05, 0x06, 0x07, 0x08, 0x00, 0x00 };
                byte[] expected3 = { 0x00, 0x02, 0x09, 0x0A, 0x00, 0x00, 0x00, 0x00 };
                mockWriteStream.Verify(x => x.Write(expected1, 0, 6), Times.Once);
                mockWriteStream.Verify(x => x.Write(expected2, 0, 6), Times.Once);
                mockWriteStream.Verify(x => x.Write(expected3, 0, 4), Times.Once);
                mockWriteStream.Verify(x => x.Flush(), Times.Exactly(3));

            }
            // new byte[0];
        }
    } 
}
