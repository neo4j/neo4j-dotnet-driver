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
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal;
using Sockets.Plugin.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests
{
    public class ChunkedInputTests
    {
        public class ReadBSyteMethod
        {
            [Theory]
            [InlineData(new byte[] {0x00, 0x01, 0x80, 0x00, 0x00}, sbyte.MinValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x7F, 0x00, 0x00}, sbyte.MaxValue)]
            [InlineData(new byte[] {0x00, 0x01, 0x00, 0x00, 0x00}, 0)]
            [InlineData(new byte[] {0x00, 0x01, 0xFF, 0x00, 0x00}, -1)]
            public void ShouldReturnTheCorrectValue(byte[] response, sbyte correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, response);

                var chunkedInput = new ChunkedInputStream(clientMock.Object, new BigEndianTargetBitConverter(), null);
                var actual = chunkedInput.ReadSByte();
                actual.Should().Be(correctValue); //, $"Got: {actual}, expected: {correctValue}");
            }
        }

        public class MultipleChunksTests
        {
            private readonly ITestOutputHelper _output;

            public MultipleChunksTests(ITestOutputHelper output)
            {
                _output = output;
            }

            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02})]
            public void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, input);

                var chunkedInput = new ChunkedInputStream(clientMock.Object, new BigEndianTargetBitConverter(), null);
                byte[] actual = new byte[3];
                chunkedInput.ReadBytes( actual );
                actual.Should().Equal(correctValue);
            }
            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldLogBytes(byte[] input, byte[] correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                var loggerMock = new Mock<ILogger>();
                loggerMock.Setup(x => x.Trace(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Callback<string, object[]>((s, o) => _output.WriteLine(s +  ((byte[])o[0]).ToHexString(showX:true)));
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, input);

                var chunkedInput = new ChunkedInputStream(clientMock.Object, new BigEndianTargetBitConverter(), loggerMock.Object);
                byte[] actual = new byte[3];
                chunkedInput.ReadBytes(actual);
                actual.Should().Equal(correctValue);
                loggerMock.Verify(x => x.Trace("S: ", It.IsAny<byte[]>()), Times.Exactly(4));
            }

            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldReadMessageBiggerThanChunkSize(byte[] input, byte[] correctValue)
            {
                var clientMock = new Mock<ITcpSocketClient>();
                TestHelper.TcpSocketClientSetup.SetupClientReadStream(clientMock, input);

                var chunkedInput = new ChunkedInputStream(clientMock.Object, new BigEndianTargetBitConverter(), null, 1);
                byte[] actual = new byte[3];
                chunkedInput.ReadBytes(actual);
                actual.Should().Equal(correctValue);
            }
        }

      
    }
}