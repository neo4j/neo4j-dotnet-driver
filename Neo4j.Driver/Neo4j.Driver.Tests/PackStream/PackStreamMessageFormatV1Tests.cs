// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Packstream;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class PackStreamMessageFormatV1Tests
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
                        .Callback<byte[], int, int>(
                            (buffer, start, size) => Received = $"{buffer.ToHexString(start, size)}");

                    MockTcpSocketClient
                        .Setup(t => t.WriteStream)
                        .Returns(MockStream.Object);
                }

                public void VerifyWrite(byte[] bytes, int bufferSize = ChunkedOutputStream.BufferSize)
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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                writer.Write(new InitMessage("a", new Dictionary<string, object>()));
                writer.Flush();

                mocks.VerifyWrite(new byte[] {0x00, 0x05, 0xB1, 0x01, 0x81, 0x61, 0xA0, 0x00, 0x00});
            }

            [Fact]
            public void PackRunMessageCorrectly()
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                writer.Write(new RunMessage("RETURN 1 AS num"));
                writer.Flush();
                mocks.VerifyWrite(
                    "00 13 b2 10  8f 52 45 54  55 52 4e 20  31 20 41 53 20 6e 75 6d  a0 00 00".ToByteArray());
            }

            [Theory]
            [InlineData(1, "00 0D B2 10 80 A1 87 69 6E 74 65 67 65 72 01 00 00")]
            [InlineData(long.MinValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 80 00 00 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 7F FF FF FF FF FF FF FF 00 00")]
            [InlineData((long) int.MinValue - 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB FF FF FF FF 7F FF FF FF 00 00")]
            [InlineData((long) int.MaxValue + 1,
                "00 15 B2 10 80 A1 87 69 6E 74 65 67 65 72 CB 00 00 00 00 80 00 00 00 00 00")]
            [InlineData(int.MinValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 80 00 00 00 00 00")]
            [InlineData(int.MaxValue, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 7F FF FF FF 00 00")]
            [InlineData(short.MinValue - 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA FF FF 7F FF 00 00")]
            [InlineData(short.MaxValue + 1, "00 11 B2 10 80 A1 87 69 6E 74 65 67 65 72 CA 00 00 80 00 00 00")]
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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"integer", value}};

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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Theory]
            [InlineData("它们的语言学归属在西方语言学界存在争议",
                "00 45 B2 10 80 A1 85 76 61 6C 75 65 D0 39 E5 AE 83 E4 BB AC E7 9A 84 E8 AF AD E8 A8 80 E5 AD A6 E5 BD 92 E5 B1 9E E5 9C A8 E8 A5 BF E6 96 B9 E8 AF AD E8 A8 80 E5 AD A6 E7 95 8C E5 AD 98 E5 9C A8 E4 BA 89 E8 AE AE 00 00"
                )]
            [InlineData("", "00 0B B2 10 80 A1 85 76 61 6C 75 65 80 00 00")]
            [InlineData("kåkåkå kå",
                "00 18 B2 10 80 A1 85 76 61 6C 75 65 8D 6B C3 A5 6B C3 A5 6B C3 A5 20 6B C3 A5 00 00")]
            public void PackRunMessageWithStringParamCorrectly(string value, string expectedBytes)
            {
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

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
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithArrayParamCorrectly()
            {
                var value = new int[] {1, 2};
                string expectedBytes = "00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithGenericListParamCorrectly()
            {
                var value = new List<int> {1, 2};
                string expectedBytes = "00 0D B2 10 80 A1 85 76 61 6C 75 65 92 01 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithDictionaryParamCorrectly()
            {
                var value = new Dictionary<string, object> {{"key1", 1}, {"key2", 2}};
                string expectedBytes =
                    "00 17 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 02 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }

            [Fact]
            public void PackRunMessageWithDictionaryMixedTypesParamCorrectly()
            {
                var value = new Dictionary<string, object> {{"key1", 1}, {"key2", "a string value"}};
                string expectedBytes =
                    " 00 25 B2 10 80 A1 85 76 61 6C 75 65 A2 84 6B 65 79 31 01 84 6B 65 79 32 8E 61 20 73 74 72 69 6E 67 20 76 61 6C 75 65 00 00";
                var mocks = new Mocks();

                var writer =
                    new PackStreamMessageFormatV1(mocks.TcpSocketClient, new BigEndianTargetBitConverter(), null).Writer;
                var values = new Dictionary<string, object> {{"value", value}};

                writer.Write(new RunMessage("", values));
                writer.Flush();
                mocks.VerifyWrite(expectedBytes.ToByteArray());
            }
        }

        public class ReaderV1Tests
        {
            public class UnpackValueMethod
            {
                [Theory]
                [InlineData(2147483648, new byte[] {0xCB, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00})]
                [InlineData(9223372036854775807, new byte[] {0xCB, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF})]
                public void UnpackLongCorrectly(long expected, byte[] data)
                {
                    UnpackNumCorrectly(expected, data);
                }

                [Theory]
                [InlineData(32768, new byte[] {0xCA, 0x00, 0x00, 0x80, 0x00})]
                public void UnpackIntCorrectly(int expected, byte[] data)
                {
                    UnpackNumCorrectly(expected, data);
                }

                [Theory]
                [InlineData(-16, new byte[] {0xF0})]
                [InlineData(42, new byte[] {0x2A})]
                [InlineData(127, new byte[] {0x7F})]
                public void UnpackTinyIntCorrectly(sbyte value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                [Theory]
                [InlineData(-128, new byte[] {0xC8, 0x80})]
                [InlineData(-17, new byte[] {0xC8, 0xEF})]
                public void UnpackInt8Correctly(sbyte value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                [Theory]
                [InlineData(128, new byte[] {0xC9, 0x00, 0x80})]
                public void UnpackShortCorrectly(short value, byte[] expected)
                {
                    UnpackNumCorrectly(value, expected);
                }

                private void UnpackNumCorrectly(long expected, byte[] data)
                {
                    var mockTcpSocketClient = new Mock<ITcpSocketClient>();

                    List<byte> bytes = new List<byte>
                    {
                        0x00,
                        (byte) data.Length
                    };
                    bytes.AddRange(data);

                    TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes.ToArray());

                    PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                        new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                            null)
                            .Reader;
                    var real = reader.UnpackValue();
                    Assert.Equal(expected, real);
                }

                public class StructUnpacker
                {
                    [Fact]
                    public void ShouldUnpackRelationshipCorrectly()
                    {
                        var bytes = "00 07 B5 52 01 02 03 80 a0 00 00".ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var real = reader.UnpackValue();
                        IRelationship rel = real as IRelationship;
                        rel.Should().NotBeNull();

                        rel.Id.Should().Be(1);
                        rel.StartNodeId.Should().Be(2);
                        rel.EndNodeId.Should().Be(3);
                        rel.Type.Should().BeEmpty();
                        rel.Properties.Should().BeEmpty();
                    }

                    [Fact]
                    public void ShouldUnpackNodeCorrectly()
                    {
                        var bytes = "00 06 B3 4E 01 90 A0 00 00".ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var node = reader.UnpackValue();
                        INode n = node as INode;
                        n.Should().NotBeNull();

                        n.Id.Should().Be(1);
                        n.Properties.Should().BeEmpty();
                        n.Labels.Should().BeEmpty();
                    }

                    [Fact]
                    public void ShouldUnpackPathCorrectly()
                    {
                        var bytes = "00 0A B3 50 91 B3 4E 01 90 A0 90 90 A0 00 00".ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Id.Should().Be(1);
                        p.Start.Properties.Should().BeEmpty();
                        p.Start.Labels.Should().BeEmpty();
                        p.Nodes.Should().HaveCount(1);
                        p.Relationships.Should().HaveCount(0);
                    }

                    [Fact]
                    public void ShouldUnpackZeroLenghPathCorrectly()
                    {
                        // A
                        var bytes =
                            "00 2C B3 50 91 B3 4E C9 03 E9    92 86 50 65 72 73 6F 6E    88 45 6D 70 6C 6F 79 65    65 A2 84 6E 61 6D 65 85 41 6C 69 63 65 83 61 67    65 21 90 90 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Alice).Should().BeTrue();

                        p.Nodes.Should().HaveCount(1);
                        p.Relationships.Should().HaveCount(0);
                    }

                    [Fact]
                    public void ShouldUnpackPathWithLenghOneCorrectly()
                    {
                        // A->B
                        var bytes =
                            "00 66 B3 50 92 B3 4E C9 03 E9    92 86 50 65 72 73 6F 6E    88 45 6D 70 6C 6F 79 65    65 A2 84 6E 61 6D 65 85 41 6C 69 63 65 83 61 67    65 21 B3 4E C9 03 EA 92    86 50 65 72 73 6F 6E 88    45 6D 70 6C 6F 79 65 65 A2 84 6E 61 6D 65 83 42    6F 62 83 61 67 65 2C 91    B3 72 0C 85 4B 4E 4F 57    53 A1 85 73 69 6E 63 65 C9 07 CF 92 01 01 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(2);
                        p.Relationships.Should().HaveCount(1);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Bob).Should().BeTrue();

                        p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
                    }

                    [Fact]
                    public void ShouldUnpackPathWithLenghTwoCorrectly()
                    {
                        // A->C->D
                        var bytes =
                            "00 73 B35093B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EC90A1846E616D65844461766592B3720D854C494B4553A0B372228A4D4152524945445F544FA09401010202 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(3);
                        p.Relationships.Should().HaveCount(2);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                        List<INode> correctOrder = new List<INode> {TestNodes.Alice, TestNodes.Carol, TestNodes.Dave};
                        p.Nodes.Should().ContainInOrder(correctOrder);

                        p.Relationships[0].Equals(TestRelationships.AliceLikesCarol).Should().BeTrue();
                        List<IRelationship> expectedRelOrder = new List<IRelationship>
                        {
                            TestRelationships.AliceLikesCarol,
                            TestRelationships.CarolMarriedToDave
                        };
                        p.Relationships.Should().ContainInOrder(expectedRelOrder);
                    }

                    [Fact]
                    public void ShouldUnpackPathWithRelationshipTraversedAgainstItsDirectionCorrectly()
                    {
                        // A->B<-C->D
                        var bytes =
                            "00 b0 B35094B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EC90A1846E616D65844461766593B3720C854B4E4F5753A18573696E6365C907CFB37220884449534C494B4553A0B372228A4D4152524945445F544FA0960101FE020303 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(4);
                        p.Relationships.Should().HaveCount(3);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                        List<INode> correctOrder = new List<INode>
                        {
                            TestNodes.Alice,
                            TestNodes.Bob,
                            TestNodes.Carol,
                            TestNodes.Dave
                        };
                        p.Nodes.Should().ContainInOrder(correctOrder);

                        p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
                        List<IRelationship> expectedRelOrder = new List<IRelationship>
                        {
                            TestRelationships.AliceKnowsBob,
                            TestRelationships.CarolDislikesBob,
                            TestRelationships.CarolMarriedToDave
                        };
                        p.Relationships.Should().ContainInOrder(expectedRelOrder);
                    }

                    [Fact]
                    public void ShouldUnpackPathWithNodeVisitedMulTimesCorrectly()
                    {
                        // A->B<-A->C->B<-C
                        var bytes =
                            "00 9E B35093B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EB9186506572736F6EA1846E616D65854361726F6C93B3720C854B4E4F5753A18573696E6365C907CFB3720D854C494B4553A0B37220884449534C494B4553A09A0101FF0002020301FD02 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(6);
                        p.Relationships.Should().HaveCount(5);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Carol).Should().BeTrue($"Got {p.End.Id}");

                        List<INode> correctOrder = new List<INode>
                        {
                            TestNodes.Alice,
                            TestNodes.Bob,
                            TestNodes.Alice,
                            TestNodes.Carol,
                            TestNodes.Bob,
                            TestNodes.Carol
                        };
                        p.Nodes.Should().ContainInOrder(correctOrder);

                        List<IRelationship> expectedRelOrder = new List<IRelationship>
                        {
                            TestRelationships.AliceKnowsBob,
                            TestRelationships.AliceKnowsBob,
                            TestRelationships.AliceLikesCarol,
                            TestRelationships.CarolDislikesBob,
                            TestRelationships.CarolDislikesBob
                        };
                        p.Relationships.Should().ContainInOrder(expectedRelOrder);
                        p.Relationships[0].Equals(TestRelationships.AliceKnowsBob).Should().BeTrue();
                    }

                    [Fact]
                    public void ShouldUnpackPathWithRelTraversedMulTimesInSameDirectionCorrectly()
                    {
                        // A->C->B<-A->C->D
                        var bytes =
                            "00 BE B35094B34EC903E99286506572736F6E88456D706C6F796565A2846E616D6585416C6963658361676521B34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EA9286506572736F6E88456D706C6F796565A2846E616D6583426F62836167652CB34EC903EC90A1846E616D65844461766594B3720D854C494B4553A0B37220884449534C494B4553A0B3720C854B4E4F5753A18573696E6365C907CFB372228A4D4152524945445F544FA09A01010202FD0001010403 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(6);
                        p.Relationships.Should().HaveCount(5);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Alice).Should().BeTrue();
                        p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                        List<INode> correctOrder = new List<INode>
                        {
                            TestNodes.Alice,
                            TestNodes.Carol,
                            TestNodes.Bob,
                            TestNodes.Alice,
                            TestNodes.Carol,
                            TestNodes.Dave
                        };
                        p.Nodes.Should().ContainInOrder(correctOrder);

                        List<IRelationship> expectedRelOrder = new List<IRelationship>
                        {
                            TestRelationships.AliceLikesCarol,
                            TestRelationships.CarolDislikesBob,
                            TestRelationships.AliceKnowsBob,
                            TestRelationships.AliceLikesCarol,
                            TestRelationships.CarolMarriedToDave
                        };
                        p.Relationships.Should().ContainInOrder(expectedRelOrder);
                        p.Relationships[0].Equals(TestRelationships.AliceLikesCarol).Should().BeTrue();
                    }

                    [Fact]
                    public void ShouldUnpackPathWithLoopCorrectly()
                    {
                        // C->D->D
                        var bytes =
                            "00 50 B35092B34EC903EB9186506572736F6EA1846E616D65854361726F6CB34EC903EC90A1846E616D65844461766592B372228A4D4152524945445F544FA0B3722C89574F524B535F464F52A09401010201 00 00"
                                .ToByteArray();
                        var mockTcpSocketClient = new Mock<ITcpSocketClient>();
                        TestHelper.TcpSocketClientSetup.SetupClientReadStream(mockTcpSocketClient, bytes);

                        PackStreamMessageFormatV1.ReaderV1 reader = (PackStreamMessageFormatV1.ReaderV1)
                            new PackStreamMessageFormatV1(mockTcpSocketClient.Object, new BigEndianTargetBitConverter(),
                                null)
                                .Reader;
                        var path = reader.UnpackValue();
                        IPath p = path as IPath;
                        p.Should().NotBeNull();

                        p.Nodes.Should().HaveCount(3);
                        p.Relationships.Should().HaveCount(2);

                        p.Start.Should().NotBeNull();
                        p.End.Should().NotBeNull();
                        p.Start.Equals(TestNodes.Carol).Should().BeTrue();
                        p.End.Equals(TestNodes.Dave).Should().BeTrue($"Got {p.End.Id}");

                        List<INode> correctOrder = new List<INode>
                        {
                            TestNodes.Carol,
                            TestNodes.Dave,
                            TestNodes.Dave
                        };
                        p.Nodes.Should().ContainInOrder(correctOrder);

                        List<IRelationship> expectedRelOrder = new List<IRelationship>
                        {
                            TestRelationships.CarolMarriedToDave,
                            TestRelationships.DaveWorksForDave,
                        };
                        p.Relationships.Should().ContainInOrder(expectedRelOrder);
                        p.Relationships[0].Equals(TestRelationships.CarolMarriedToDave).Should().BeTrue();
                    }

                    private static class TestNodes
                    {
                        public static INode Alice = new Node(1001L,
                            new List<string> {"Person", "Employee"},
                            new Dictionary<string, object> {{"name", "Alice"}, {"age", 33l}});

                        public static INode Bob = new Node(1002L,
                            new List<string> {"Person", "Employee"},
                            new Dictionary<string, object> {{"name", "Bob"}, {"age", 44l}});

                        public static INode Carol = new Node(
                            1003L,
                            new List<string> {"Person"},
                            new Dictionary<string, object> {{"name", "Carol"}});

                        public static INode Dave = new Node(
                            1004L,
                            new List<string>(),
                            new Dictionary<string, object> {{"name", "Dave"}});
                    }

                    private static class TestRelationships
                    {
                        // IRelationship types
                        private static string KNOWS = "KNOWS";
                        private static string LIKES = "LIKES";
                        private static string DISLIKES = "DISLIKES";

                        private static string MARRIED_TO =
                            "MARRIED_TO";

                        private static string WORKS_FOR =
                            "WORKS_FOR";

                        // IRelationships
                        public static IRelationship AliceKnowsBob =
                            new Relationship( 12L, TestNodes.Alice.Id,
                                TestNodes.Bob.Id, KNOWS,
                                new Dictionary<string, object> {{"since", 1999L}});

                        public static IRelationship AliceLikesCarol =
                            new Relationship(13L, TestNodes.Alice.Id,
                                TestNodes.Carol.Id, LIKES,
                                new Dictionary<string, object>());

                        public static IRelationship CarolDislikesBob =
                            new Relationship(32L, TestNodes.Carol.Id,
                                TestNodes.Bob.Id, DISLIKES,
                                new Dictionary<string, object>());

                        public static IRelationship CarolMarriedToDave =
                            new Relationship(34L, TestNodes.Carol.Id,
                                TestNodes.Dave.Id, MARRIED_TO,
                                new Dictionary<string, object>());

                        public static IRelationship DaveWorksForDave =
                            new Relationship(44L, TestNodes.Dave.Id,
                                TestNodes.Dave.Id, WORKS_FOR,
                                new Dictionary<string, object>());
                    }
                }
            }
        }
    }
}