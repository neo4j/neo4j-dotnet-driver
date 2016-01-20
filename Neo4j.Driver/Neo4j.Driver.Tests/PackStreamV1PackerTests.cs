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
using System.Collections;
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
            private class Mocks
            {
                public Mock<Stream> MockStream { get; }
                public Mock<ITcpSocketClient> MockTcpSocketClient { get; }
                public Stream Stream => MockStream.Object;
                public ITcpSocketClient TcpSocketClient => MockTcpSocketClient.Object;
                
                public string Received { get; set; }

                public Mocks()
                {
                    MockTcpSocketClient = new Mock<ITcpSocketClient>();
                    MockStream = new Mock<Stream>();

                    MockStream
                        .Setup(s => s.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()))
                        .Callback<byte[], int, int>((buffer, start, size) => Received = $"{buffer.ToHexString(start, size)}");

                    MockTcpSocketClient
                        .Setup(t => t.WriteStream)
                        .Returns(MockStream.Object);

                }

                public void VerifyWrite(byte[] bytes, int bufferSize = PackStreamV1ChunkedOutput.BufferSize)
                {
                    byte[] expectedBytes = bytes.PadRight(bufferSize);
                    MockStream.Verify(c => c.Write(expectedBytes, 0, It.IsAny<int>()), Times.Once,
                        $"Received {Received}{Environment.NewLine}Expected {expectedBytes.ToHexString(0, bytes.Length)}");
                }
            }
            
            [Fact]
            public void PacksInitMessageCorrectly()
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                writer.Write(new InitMessage("a"));
                writer.Flush();

                mocks.VerifyWrite(new byte[] { 0x00, 0x04, 0xB1, 0x01, 0x81, 0x61, 0x00, 0x00 });
            }

            [Fact]
            public void PackRunMessageCorrectly()
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                writer.Write(new RunMessage("RETURN 1 AS num"));
                writer.Flush();
                mocks.VerifyWrite("00 13 b2 10  8f 52 45 54  55 52 4e 20  31 20 41 53 20 6e 75 6d  a0 00 00".ToByteArray());
            }

            [Theory]
            [InlineData( 1, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 01 00 00")]
            [InlineData(long.MinValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 80 00 00 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 7F FF FF FF FF FF FF FF 00 00")]
            [InlineData((long)int.MinValue-1, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB FF FF FF FF 7F FF FF FF 00 00")]
            [InlineData((long)int.MaxValue+1, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 00 00 00 00 80 00 00 00 00 00")]
            [InlineData(int.MinValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 80 00 00 00 00 00")]
            [InlineData(int.MaxValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 7F FF FF FF 00 00")]
            [InlineData(short.MinValue-1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA FF FF 7F FF 00 00")]
            [InlineData(short.MaxValue+1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 00 00 80 00 00 00")]
            [InlineData(short.MinValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 80 00 00 00")]
            [InlineData(short.MaxValue, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 7F FF 00 00")]
            [InlineData(-129, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 FF 7F 00 00")]
            [InlineData(128, "00 0F B2 10 80 A1 87 69 6E 74 65 67 65 72 C9 00 80 00 00")]
            [InlineData(-128, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 80 00 00")]
            [InlineData(-17, "00 0E B2 10 80 A1 87 69 6E 74 65 67 65 72 C8 EF 00 00")]
            [InlineData(-16, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 F0 00 00")]
            [InlineData(127, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 7F 00 00")]
            public void PackRunMessageWithIntegerParamCorrectly(long value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "integer", value }};
               
                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }
            [Theory]
            [InlineData(true, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C3 00 00")]
            [InlineData(false, "00 0B B2 10 80 A1 85 76 61 6C 75 65 C2 00 00")]
            public void PackRunMessageWithBoolParamCorrectly(bool value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value} };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Theory]
            [InlineData(1.00, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(double.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 7F EF FF FF FF FF FF FF 00 00")]
            [InlineData(double.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 FF EF FF FF FF FF FF FF 00 00")]
            public void PackRunMessageWithDoubleParamCorrectly(double value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Theory]
            [InlineData(1.0f, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 3F F0 00 00 00 00 00 00 00 00")]
            [InlineData(float.MaxValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 47 EF FF FF E0 00 00 00 00 00")]
            [InlineData(float.MinValue, "00 13 B2 10 80 A1 85 76 61 6C 75 65 C1 C7 EF FF FF E0 00 00 00 00 00")]
            public void PackRunMessageWithFloatParamCorrectly(float value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Theory]
            [InlineData("它们的语言学归属在西方语言学界存在争议", "00 45 B2 10 80 A1 85 76 61 6C 75 65 D0 39 E5 AE 83 E4 BB AC E7 9A 84 E8 AF AD E8 A8 80 E5 AD A6 E5 BD 92 E5 B1 9E E5 9C A8 E8 A5 BF E6 96 B9 E8 AF AD E8 A8 80 E5 AD A6 E7 95 8C E5 AD 98 E5 9C A8 E4 BA 89 E8 AE AE 00 00")]
            [InlineData("", "00 0B B2 10 80 A1 85 76 61 6C 75 65 80 00 00")]
            [InlineData("kåkåkå kå", "00 18 B2 10 80 A1 85 76 61 6C 75 65 8D 6B C3 A5 6B C3 A5 6B C3 A5 20 6B C3 A5 00 00")]
            public void PackRunMessageWithStringParamCorrectly(string value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithArrayListParamCorrectly()
            {
                var value = new ArrayList();
                string expectedBytes = "00 0B B2 10 80 A1 85 76 61 6C 75 65 90 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithArrayParamCorrectly()
            {
                var value = new int[] {1,2};
                string expectedBytes = "00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithGenericListParamCorrectly()
            {
                var value = new List<int> { 1, 2 };
                string expectedBytes= "00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithDictionaryParamCorrectly()
            {
                var value = new Dictionary<string, object>{{ "key1", 1}, {"key2", 2}};
                string expectedBytes = "00 17 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithDictionaryMixedTypesParamCorrectly()
            {
                var value = new Dictionary<string, object> { { "key1", 1 }, { "key2", "a string value" } };
                string expectedBytes = " 00 25 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 8E 61 20 73 74 72 69 6E 67 20 76 61 6C 75 65 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter()).Writer;
                var values = new Dictionary<string, object> { { "value", value } };

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
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