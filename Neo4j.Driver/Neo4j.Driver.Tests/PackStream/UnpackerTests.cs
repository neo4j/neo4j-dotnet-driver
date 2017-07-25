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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public partial class PackStreamTests
    {

        private static Mock<MemoryStream> CreateMockStream(byte b)
        {
            return CreateMockStream(new byte[] {b});
        }

        private static Mock<MemoryStream> CreateMockStream(params byte[][] bytes)
        {
            MemoryStream tmpStream = new MemoryStream();
            foreach (var b in bytes)
            {
                tmpStream.Write(b, 0, b.Length);
            }

            var mockInput = new Mock<MemoryStream>(tmpStream.ToArray());

            mockInput.Setup(x => x.Length).CallBase();
            mockInput.Setup(x => x.Position).CallBase();
            mockInput.Setup(x => x.CanRead).CallBase();
            mockInput.Setup(x => x.ReadByte()).CallBase();
            mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).CallBase();

            return mockInput;
        }

        public class UnpackerTests
        {
            public class UnpackNullMethod
            {
                [Fact]
                public void ShouldUnpackNullSuccessfully()
                {
                    var mockInput = CreateMockStream(PackStream.NULL);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackNull().Should().BeNull();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }


                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotNull()
                {
                    var mockInput = CreateMockStream(PackStream.BYTES_16);

                    var unpacker = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackNull());
                    ex.Should().BeOfType<ProtocolException>();
                }
            }
            
            public class UnpackBooleanMethod
            {
                [Fact]
                public void ShouldUnpackBooleanTrueSuccessfully()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] b, int o, int c) => b[0] = PackStream.TRUE)
                        .Returns(1);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackBoolean().Should().BeTrue();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackBooleanFalseSuccessfully()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] b, int o, int c) => b[0] = PackStream.FALSE)
                        .Returns(1);
                        
                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackBoolean().Should().BeFalse();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }


                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotTrueOrFalse()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] b, int o, int c) => b[0] = PackStream.BYTES_16)
                        .Returns(1);

                    var unpacker = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackBoolean());
                    ex.Should().BeOfType<ProtocolException>();
                }
            }
            
            public class UnpackLongMethod
            {
                [Theory]
                [InlineData(0xF0, -16)]
                [InlineData(0xFF, -1)]
                [InlineData(0x7F, 127)] // 7F to FF
                public void ShouldUnpackLongAsTinyByte(byte data, sbyte expected)
                {
                    var mockInput = CreateMockStream(data);

                    var u = new PackStreamReader(mockInput.Object);

                    sbyte real = (sbyte)u.UnpackLong();
                    real.Should().Be(expected);

                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackLongAsSignedByte()
                {
                    sbyte expected = 1;

                    var mockInput = CreateMockStream(new byte[] { PackStream.INT_8, (byte)expected });

                    var u = new PackStreamReader(mockInput.Object);

                    sbyte real = (sbyte) u.UnpackLong();
                    Assert.Equal(expected, real);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackLongAsShort()
                {
                    short expected = 124;

                    var mockInput = CreateMockStream(new byte[] { PackStream.INT_16 },
                        PackStreamBitConverter.GetBytes(expected));

                    var u = new PackStreamReader(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackLongAsInt()
                {
                    int expected = 1024;

                    var mockInput = CreateMockStream(new byte[] { PackStream.INT_32 },
                        PackStreamBitConverter.GetBytes(expected));

                    var u = new PackStreamReader(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackLongAsLong()
                {
                    long expected = 1024;

                    var mockInput = CreateMockStream(new byte[] { PackStream.INT_64 },
                        PackStreamBitConverter.GetBytes(expected));

                    var u = new PackStreamReader(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotLong()
                {
                    var mockInput = CreateMockStream(PackStream.BYTES_16);

                    var unpacker = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackLong());
                    ex.Should().BeOfType<ProtocolException>();
                }
            }
            
            public class UnpackDoubleMethod
            {
                [Fact]
                public void ShouldUnpackDoubleCorrectly()
                {
                    double expected = 1.12;

                    var mockInput = CreateMockStream(new byte[] {PackStream.FLOAT_64},
                        PackStreamBitConverter.GetBytes(expected));

                    var u = new PackStreamReader(mockInput.Object);

                    Assert.Equal(expected, u.UnpackDouble());
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotDouble()
                {
                    var mockInput = CreateMockStream(PackStream.BYTES_16);

                    var unpacker = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackDouble());
                    ex.Should().BeOfType<ProtocolException>();
                }
            }
            
            public class UnpackStringMethod
            {
                [Fact]
                public void ShouldUnpackTinyStringAsEmptyString()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.TINY_STRING});

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackString().Should().BeEmpty(); //.Equals(String.Empty);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackStringLessThan16Chars()
                {
                    var mockInput = CreateMockStream(new byte[] { 0x81 }, new byte[] { 0x61 });

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackString8()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.STRING_8, (byte) 1 },
                        new byte[] { 0x61 });

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldUnpackString16()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.STRING_16},
                        PackStreamBitConverter.GetBytes((short) 1), new byte[] {0x61});

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldUnpackString32()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.STRING_32},
                        PackStreamBitConverter.GetBytes((int) 1), new byte[] {0x61});

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldThrowExceptionWhenUnpackString32ReturnsStringSizeLonggerThanIntMax()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.STRING_32 },
                        PackStreamBitConverter.GetBytes((int)-1));

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackString());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotString()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.FALSE });

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackString());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }
            }

            public class UnpackByteMethod
            {

                [Fact]
                public void ShouldUnpackBytes8()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.BYTES_8, 1 }, new byte[] { 0x61 });

                    var u = new PackStreamReader(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldUnpackBytes16()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.BYTES_16},
                        PackStreamBitConverter.GetBytes((short) 1), new byte[] {0x61});

                    var u = new PackStreamReader(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldUnpackBytes32()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.BYTES_32 },
                        PackStreamBitConverter.GetBytes((int)1), new byte[] { 0x61 });

                    var u = new PackStreamReader(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
                }

                [Fact]
                public void ShouldThrowExceptionWhenUnpackBytes32ReturnsBytesSizeLonggerThanIntMax()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.BYTES_32 },
                        PackStreamBitConverter.GetBytes((int)-1));

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackBytes());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotBytes()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] b, int o, int c) => b[0] = PackStream.FALSE)
                        .Returns(1);

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackBytes());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }
            }
            
            public class UnpackMapHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyMap()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(0xA2);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackMap8()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.MAP_8, (byte)1 });

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackMap16()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.MAP_16);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), 2))
                        .Callback((byte[] b, int o, int c) => PackStreamBitConverter.GetBytes((short)1).CopyTo(b, o))
                        .Returns((byte[] b, int o, int c) => c);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackMap32()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.MAP_32);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), 4))
                        .Callback((byte[] b, int o, int c) => PackStreamBitConverter.GetBytes((int)-1).CopyTo(b, o))
                        .Returns((byte[] b, int o, int c) => c);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(uint.MaxValue);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotMap()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackMapHeader());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }
            
            public class UnpackListHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyList()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(0x92);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackListHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackList8()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.LIST_8, (byte) 1});

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackListHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackList16()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.LIST_16);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), 2))
                        .Callback((byte[] b, int o, int c) => PackStreamBitConverter.GetBytes((short)1).CopyTo(b, o))
                        .Returns((byte[] b, int o, int c) => c);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackListHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackList32()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.LIST_32);
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), 4))
                        .Callback((byte[] b, int o, int c) => PackStreamBitConverter.GetBytes((int)-1).CopyTo(b, o))
                        .Returns((byte[] b, int o, int c) => c);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackListHeader().Should().Be(uint.MaxValue);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotList()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackListHeader());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }
            
            public class UnpackStructSignature
            {
                [Fact]
                public void ShouldCallReadByteOnce()
                {
                    var mockInput = new Mock<MemoryStream>(new byte[] {0xFF});
                    mockInput.Setup(x => x.CanRead).CallBase();
                    mockInput.Setup(x => x.ReadByte()).CallBase();
                    mockInput.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).CallBase();

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackStructSignature().Should().Be(0xFF);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }
            }


            public class UnpackStructHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyStruct()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(0xB2);

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackStruct8()
                {
                    var mockInput = CreateMockStream(new byte[] {PackStream.STRUCT_8, (byte) 1});

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackStruct16()
                {
                    var mockInput = CreateMockStream(new byte[] { PackStream.STRUCT_16 }, PackStreamBitConverter.GetBytes((short)1));

                    var u = new PackStreamReader(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotStruct()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackStructHeader());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }
            
            public class PeekNextTypeMethod
            {
                [Theory]
                [InlineData(PackStream.TINY_STRING | 0x08, PackStream.PackType.String)]
                [InlineData(PackStream.TINY_LIST | 0x08, PackStream.PackType.List)]
                [InlineData(PackStream.TINY_MAP | 0x08, PackStream.PackType.Map)]
                [InlineData(PackStream.TINY_STRUCT | 0x08, PackStream.PackType.Struct)]
                [InlineData(PackStream.NULL, PackStream.PackType.Null)]
                [InlineData(PackStream.TRUE, PackStream.PackType.Boolean)]
                [InlineData(PackStream.FALSE, PackStream.PackType.Boolean)]
                [InlineData(PackStream.FLOAT_64 , PackStream.PackType.Float)]
                [InlineData(PackStream.BYTES_8, PackStream.PackType.Bytes)]
                [InlineData(PackStream.BYTES_16, PackStream.PackType.Bytes)]
                [InlineData(PackStream.BYTES_32, PackStream.PackType.Bytes)]
                [InlineData(PackStream.STRING_8, PackStream.PackType.String)]
                [InlineData(PackStream.STRING_16, PackStream.PackType.String)]
                [InlineData(PackStream.STRING_32, PackStream.PackType.String)]
                [InlineData(PackStream.LIST_8, PackStream.PackType.List)]
                [InlineData(PackStream.LIST_16, PackStream.PackType.List)]
                [InlineData(PackStream.LIST_32, PackStream.PackType.List)]
                [InlineData(PackStream.MAP_8, PackStream.PackType.Map)]
                [InlineData(PackStream.MAP_16, PackStream.PackType.Map)]
                [InlineData(PackStream.MAP_32, PackStream.PackType.Map)]
                [InlineData(PackStream.STRUCT_8, PackStream.PackType.Struct)]
                [InlineData(PackStream.STRUCT_16, PackStream.PackType.Struct)]
                [InlineData(PackStream.INT_8, PackStream.PackType.Integer)]
                [InlineData(PackStream.INT_16, PackStream.PackType.Integer)]
                [InlineData(PackStream.INT_32, PackStream.PackType.Integer)]
                [InlineData(PackStream.INT_64, PackStream.PackType.Integer)]
                internal void ShouldPeekTypeCorrectly(byte marker, PackStream.PackType expected)
                {
                    var mockInput = CreateMockStream(marker);

                    var u = new PackStreamReader(mockInput.Object);

                    u.PeekNextType().Should().Be(expected);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteUnDefined()
                {
                    var mockInput = new Mock<Stream>();
                    mockInput.Setup(x => x.CanRead).Returns(true);
                    mockInput.Setup(x => x.Length).Returns(1);
                    mockInput.Setup(x => x.Position).Returns(0);
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.RESERVED_C4);

                    var u = new PackStreamReader(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.PeekNextType());
                    ex.Should().BeOfType<ProtocolException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }
        }
    }
}