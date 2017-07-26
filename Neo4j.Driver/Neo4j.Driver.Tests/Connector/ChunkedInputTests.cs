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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.Tests.TcpSocketClientTestSetup;

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
                var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(response);
                var actual = chunkedInput.NextSByte();
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
                var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);

                byte[] actual = chunkedInput.ReadBytes(3);

                actual.Should().Equal(correctValue);
            }

            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldLogBytes(byte[] input, byte[] correctValue)
            {
                var loggerMock = new Mock<ILogger>();
                loggerMock.Setup(x => x.Trace(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<string, object[]>((s, o) => _output.WriteLine(s + ((byte[])o[0]).ToHexString(showX: true)));

                var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input, loggerMock.Object);

                byte[] actual = chunkedInput.ReadBytes(3);
                actual.Should().Equal(correctValue);
                loggerMock.Verify(x => x.Trace("S: ", It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
            }

            [Theory]
            //-----------------------|---head1--|----|---head2---|-----------|--msg end--|
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldReadMessageBiggerThanChunkSize(byte[] input, byte[] correctValue)
            {
                var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);

                byte[] actual = chunkedInput.ReadBytes(3);
                actual.Should().Equal(correctValue);
            }
        }

        public class ChunkHeaderTests
        {
            private readonly Random _random = new Random();
            public byte Getbyte()
            {
                var num = _random.Next(0, 26); // 0 to 25
                byte letter = (byte)('a' + num);
                return letter;
            }

            [Fact]
            public void ShouldReadHeaderWithinUnsignedShortRange()
            {
                for (var i = 1; i <= UInt16.MaxValue; i = (i << 1) + 1) // i: [0x1, 0xFFFF]
                {
                    ushort chunkHeaderSize = (ushort)(i & 0xFFFF);

                    var input = new byte[chunkHeaderSize + 2 + 2]; // 0xXX, 0xXX, ..., 0x00, 0x00
                    input[0] = (byte)((chunkHeaderSize & 0xFF00) >> 8);
                    input[1] = (byte)(chunkHeaderSize & 0xFF);
                    for (int j = 2; j < chunkHeaderSize + 2; j++)
                    {
                        input[j] = Getbyte();
                    }

                    var chunkedInput = IOExtensions.CreateChunkedPackStreamReaderFromBytes(input);
                    byte[] actual = chunkedInput.ReadBytes(chunkHeaderSize);
                    for (int j = 0; j < actual.Length; j++)
                    {
                        actual[j].Should().Be(input[2 + j]);
                    }
                }
            }
        }
      
    }
}