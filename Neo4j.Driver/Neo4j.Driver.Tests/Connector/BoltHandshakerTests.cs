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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class BoltHandshakerTests
{ 
    [Theory]
    //Server response Legacy Handshake
    [InlineData(new byte[] { 0x00, 0x00, 4, 4 }, 4, 4)] //Capabilities not used in this test 
    //Modern negotiation with manifest
    [InlineData(new byte[]
    {
        0x00, 0x00, 0x01, 0xFF,     //Identifies this as a modern negotiation with manifest v1
        0x03,                       //3 versions to follow
        0x00, 0x04, 0x04, 0x04,     //Supports 4.0-4.4
        0x00, 0x05, 0x05, 0x05,     //Supports 5.0-5.5
        0x00, 0x00, 0x07, 0x05,     //Supports version 5.7
        0x8F,                       //Support capabilities 1-4 inclusive
        0x01                        //Support capability 8
    }, 5, 7)] 
    //Should handle zero capabilities
    [InlineData(
        new byte[]
        {
            0x00, 0x00, 0x01, 0xFF, //Identifies this as a modern negotiation with manifest v1
            0x01, //1 versions to follow
            0x00, 0x00, 0x07, 0x05, //Supports version 5.7
            0x00, //no capability flags set
        },
        5,
        7)]
    [InlineData(
        new byte[]
        {
            0x00, 0x00, 0x01, 0xFF, //Identifies this as a modern negotiation with manifest v1
            0x01, //1 versions to follow
            0x00, 0x05, 0x07, 0x05, //Supports version 5.7 - 5.0
            0x00, //no capability flags set
        },
        5,
        7)]      
    private async Task DoHandshakeAsyncShouldReturnBoltVersion(byte[] streamData, int majorVersion, int minorVersion)
    {
        var version = new BoltProtocolVersion(majorVersion, minorVersion);
        var readerStream = new MemoryStream(streamData);
        var socket = new Mock<ITcpSocketClient>();
        var writerStream = new MemoryStream();

        socket.SetupGet(x => x.WriterStream).Returns(writerStream);
        socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

        var boltProtocolVersion = await BoltHandshaker.Default.DoHandshakeAsync(
            socket.Object,
            new Mock<ILogger>().Object,
            CancellationToken.None);

        boltProtocolVersion.Should().Be(version);
    }
    
   [Fact]
    private async Task DoHandshakeAsyncShouldSelectBoltVersionInRange()
    {   
        var minorVersionPlus = (byte)(BoltProtocolVersion.LatestVersion.MinorVersion + 3);
        var majorVersion = (byte)BoltProtocolVersion.LatestVersion.MajorVersion;
        var inputData = new byte[]
        {
            0x00, 0x00, 0x01, 0xFF, //Identifies this as a modern negotiation with manifest v1
            0x02, //2 versions to follow
            0x00, 0x04, 0x04, 0x04,     //4.4 -> 4.0 (range 4)   
            0x00, minorVersionPlus, minorVersionPlus, majorVersion, //set to higher than actually supported. latest version + 3 minors -> latest version first major release (e.g. 6.3 -> 6.0 where 6.0 is the current highest supported version). 
            0x00, //no capability flags set
        };

        var readerStream = new MemoryStream(inputData);
        var socket = new Mock<ITcpSocketClient>();
        var writerStream = new MemoryStream();

        socket.SetupGet(x => x.WriterStream).Returns(writerStream);
        socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

        var boltProtocolVersion = await BoltHandshaker.Default.DoHandshakeAsync(
            socket.Object,
            new Mock<ILogger>().Object,
            CancellationToken.None);

        boltProtocolVersion.Should().Be(new BoltProtocolVersion(BoltProtocolVersion.LatestVersion.MajorVersion, 
                                                                BoltProtocolVersion.LatestVersion.MinorVersion));
    }
     
    [Fact]
    public async Task DoHandshakeAsyncShouldThrowIfNotCorrectLengthResult()
    {
        var socket = new Mock<ITcpSocketClient>();
        var writerStream = new MemoryStream();
        socket.SetupGet(x => x.WriterStream).Returns(writerStream);
        var readerStream = new MemoryStream(new byte[] { 0x00, 0x00, 4 });
        socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

        var exception = await Record.ExceptionAsync(
            () => BoltHandshaker.Default.DoHandshakeAsync(
                socket.Object,
                new Mock<ILogger>().Object,
                CancellationToken.None));

        exception.Should().BeOfType<IOException>();
    }

    [Theory]
    //Should throw on unrecognized  manifest version
    [InlineData(
        new byte[]
        {
            0x00, 0x00, 0x02, 0xFF, //Identifies this as a modern negotiation with manifest v2 which is not known - ERROR
            0x01, //1 versions to follow
            0x00, 0x00, 0x07, 0x05, //Supports version 5.7
            0x00, //no capability flags set
        })]  
    //Should throw on zero number protocols being supplied
    [InlineData(
        new byte[]
        {
            0x00, 0x00, 0x02, 0xFF, //Identifies this as a modern negotiation with manifest v2
            0x00 //0 versions to follow  - ERROR   
        })]
    private async Task DoHandshakeAsyncShouldThrowProtocolException(
        byte[] streamData)
    {
        var readerStream = new MemoryStream(streamData);
        var socket = new Mock<ITcpSocketClient>();
        var writerStream = new MemoryStream();

        socket.SetupGet(x => x.WriterStream).Returns(writerStream);
        socket.SetupGet(x => x.ReaderStream).Returns(readerStream);

        var exception = await Record.ExceptionAsync(() => BoltHandshaker.Default.DoHandshakeAsync(
            socket.Object,
            new Mock<ILogger>().Object,
            CancellationToken.None)).ConfigureAwait(false);

        exception.Should().BeOfType<ProtocolException>();    
    }
}
