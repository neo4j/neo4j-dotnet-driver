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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Packstream;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public partial class PackStreamTests
    {
        public class UnpackerTests
        {
            public class UnpackNullMethod
            {
                [Fact]
                public void ShouldUnpackNullSuccessfully()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.NULL);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackNull().Should().BeNull();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }


                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotNull()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_16);

                    var unpacker = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackNull());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                }
            }

            public class UnpackBooleanMethod
            {
                [Fact]
                public void ShouldUnpackBooleanTrueSuccessfully()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.TRUE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackBoolean().Should().BeTrue();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackBooleanFalseSuccessfully()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackBoolean().Should().BeFalse();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }


                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotTrueOrFalse()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_16);

                    var unpacker = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackBoolean());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
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
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(data);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    sbyte real = (sbyte)u.UnpackLong();
                    real.Should().Be(expected);

                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadSByte(), Times.Never);
                }

                [Fact]
                public void ShouldUnpackLongAsSignedByte()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.INT_8);
                    sbyte expected = 1;
                    mockInput.Setup(x => x.ReadSByte()).Returns(expected);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    sbyte real = (sbyte) u.UnpackLong();
                    Assert.Equal(expected, real);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadSByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackLongAsShort()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.INT_16);
                    short expected = 124;
                    mockInput.Setup(x => x.ReadShort()).Returns(expected);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackLongAsInt()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.INT_32);
                    int expected = 1024;
                    mockInput.Setup(x => x.ReadInt()).Returns(expected);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackLongAsLong()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.INT_64);
                    long expected = 1024;
                    mockInput.Setup(x => x.ReadLong()).Returns(expected);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    Assert.Equal(expected, u.UnpackLong());
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadLong(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotLong()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_16);

                    var unpacker = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackLong());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                }
            }

            public class UnpackDoubleMethod
            {
                [Fact]
                public void ShouldUnpackDoubleCorrectly()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FLOAT_64);
                    double expected = 1.12;
                    mockInput.Setup(x => x.ReadDouble()).Returns(expected);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    Assert.Equal(expected, u.UnpackDouble());
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadDouble(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotDouble()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_16);

                    var unpacker = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => unpacker.UnpackDouble());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                }
            }

            public class UnpackStringMethod
            {
                [Fact]
                public void ShouldUnpackTinyStringAsEmptyString()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.TINY_STRING);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackString().Should().BeEmpty(); //.Equals(String.Empty);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackStringLessThan16Chars()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(0x81);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackString8()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte())
                        .Returns(new Queue<byte>(new[] {PackStream.STRING_8, (byte) 1}).Dequeue);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.ReadByte(), Times.Exactly(2));
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackString16()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.STRING_16);
                    mockInput.Setup(x => x.ReadShort()).Returns(1);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackString32()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.STRING_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(1);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackString().Should().Be("a");
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionWhenUnpackString32ReturnsStringSizeLonggerThanIntMax()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.STRING_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(-1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackString());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotString()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackString());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }

            public class UnpackByteMethod
            {

                [Fact]
                public void ShouldUnpackBytes8()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte())
                        .Returns(new Queue<byte>(new[] {PackStream.BYTES_8, (byte) 1}).Dequeue);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.ReadByte(), Times.Exactly(2));
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackBytes16()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_16);
                    mockInput.Setup(x => x.ReadShort()).Returns(1);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldUnpackBytes32()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(1);
                    mockInput.Setup(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()))
                        .Callback<byte[], int, int?>((buffer, offset, size) => { buffer[0] = 0x61; });

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var actual = u.UnpackBytes();
                    actual.Length.Should().Be(1);
                    actual.Should().Contain(0x61);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                    mockInput.Verify(x => x.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int?>()), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionWhenUnpackBytes32ReturnsBytesSizeLonggerThanIntMax()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.BYTES_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(-1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackBytes());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotBytes()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackBytes());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }

            public class UnpackMapHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyMap()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(0xA2);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackMap8()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte())
                        .Returns(new Queue<byte>(new[] {PackStream.MAP_8, (byte) 1}).Dequeue);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackMap16()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.MAP_16);
                    mockInput.Setup(x => x.ReadShort()).Returns(1);
                   
                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackMap32()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.MAP_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(-1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackMapHeader().Should().Be(uint.MaxValue);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotMap()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackMapHeader());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }

            public class UnpackListHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyList()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(0x92);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackListHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackList8()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte())
                        .Returns(new Queue<byte>(new[] { PackStream.LIST_8, (byte)1 }).Dequeue);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackListHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackList16()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.LIST_16);
                    mockInput.Setup(x => x.ReadShort()).Returns(1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackListHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackList32()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.LIST_32);
                    mockInput.Setup(x => x.ReadInt()).Returns(-1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackListHeader().Should().Be(uint.MaxValue);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadInt(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotList()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackListHeader());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }

            public class UnpackStructSignature
            {
                [Fact]
                public void ShouldCallReadByteOnce()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(0xFF);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackStructSignature().Should().Be(0xFF);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }
            }


            public class UnpackStructHeaderMethod
            {
                [Fact]
                public void ShouldUnpackTinyStruct()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(0xB2);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(2);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                }

                [Fact]
                public void ShouldUnpackStruct8()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte())
                        .Returns(new Queue<byte>(new[] { PackStream.STRUCT_8, (byte)1 }).Dequeue);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Exactly(2));
                }

                [Fact]
                public void ShouldUnpackStruct16()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.STRUCT_16);
                    mockInput.Setup(x => x.ReadShort()).Returns(1);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.UnpackStructHeader().Should().Be(1);
                    mockInput.Verify(x => x.ReadByte(), Times.Once);
                    mockInput.Verify(x => x.ReadShort(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteNotStruct()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.ReadByte()).Returns(PackStream.FALSE);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.UnpackStructHeader());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
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
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.PeekByte()).Returns(marker);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    u.PeekNextType().Should().Be(expected);
                    mockInput.Verify(x => x.PeekByte(), Times.Once);
                }

                [Fact]
                public void ShouldThrowExceptionIfMarkerByteUnDefined()
                {
                    var mockInput = new Mock<IInputStream>();
                    mockInput.Setup(x => x.PeekByte()).Returns(PackStream.RESERVED_C4);

                    var u = new PackStream.Unpacker(mockInput.Object);

                    var ex = Xunit.Record.Exception(() => u.PeekNextType());
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                    mockInput.Verify(x => x.PeekByte(), Times.Once);
                }
            }
        }
    }
}