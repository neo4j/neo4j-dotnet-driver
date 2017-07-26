using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class PackStreamReaderTests
    {


        public class ReadNullMethod
        {
            [Fact]
            public void ShouldReadNullSuccessfully()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.NULL);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadNull();

                real.Should().BeNull();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }


            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotNull()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_16);

                var unpacker = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => unpacker.ReadNull());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadBooleanMethod
        {
            [Fact]
            public void ShouldReadBooleanTrueSuccessfully()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.TRUE);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadBoolean();

                real.Should().BeTrue();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadBooleanFalseSuccessfully()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadBoolean();

                real.Should().BeFalse();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }


            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotTrueOrFalse()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_16);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadBoolean());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadLongMethod
        {
            [Theory]
            [InlineData(0xF0, -16)]
            [InlineData(0xFF, -1)]
            [InlineData(0x7F, 127)] // 7F to FF
            public void ShouldReadLongAsTinyByte(byte data, sbyte expected)
            {
                var mockInput = IOExtensions.CreateMockStream(data);
                var reader = new PackStreamReader(mockInput.Object);

                var real = (sbyte)reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadLongAsSignedByte()
            {
                const sbyte expected = 1;
                var mockInput = IOExtensions.CreateMockStream(PackStream.INT_8, (byte) expected);
                var reader = new PackStreamReader(mockInput.Object);

                var real = (sbyte)reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsShort()
            {
                const short expected = 124;
                var mockInput = IOExtensions.CreateMockStream(PackStream.INT_16, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsInt()
            {
                const int expected = 1024;
                var mockInput = IOExtensions.CreateMockStream(PackStream.INT_32, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsLong()
            {
                const long expected = 1024;
                var mockInput = IOExtensions.CreateMockStream(PackStream.INT_64, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotLong()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_16);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadLong());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadDoubleMethod
        {
            [Fact]
            public void ShouldReadDoubleCorrectly()
            {
                const double expected = 1.12;
                var mockInput = IOExtensions.CreateMockStream(PackStream.FLOAT_64, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadDouble();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotDouble()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_16);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadDouble());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadStringMethod
        {
            [Fact]
            public void ShouldReadTinyStringAsEmptyString()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.TINY_STRING);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadString();

                real.Should().BeEmpty(); //.Equals(String.Empty);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadStringLessThan16Chars()
            {
                var mockInput = IOExtensions.CreateMockStream(0x81, 0x61);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadString8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.STRING_8, (byte) 1, 0x61);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadString16()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] {PackStream.STRING_16},
                    PackStreamBitConverter.GetBytes((short) 1), new byte[] {0x61});
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadString32()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.STRING_32 },
                    PackStreamBitConverter.GetBytes((int)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldThrowExceptionWhenReadString32ReturnsStringSizeLonggerThanIntMax()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.STRING_32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadString());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotString()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadString());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }
        }

        public class ReadByteMethod
        {

            [Fact]
            public void ShouldReadBytes8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_8, 1, 0x61);
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadBytes16()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.BYTES_16 },
                    PackStreamBitConverter.GetBytes((short)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadBytes32()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.BYTES_32 },
                    PackStreamBitConverter.GetBytes((int)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldThrowExceptionWhenReadBytes32ReturnsBytesSizeLonggerThanIntMax()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.BYTES_32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadBytes());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotBytes()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadBytes());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }
        }

        public class ReadMapHeaderMethod
        {
            [Fact]
            public void ShouldReadTinyMap()
            {
                var mockInput = IOExtensions.CreateMockStream(0xA2);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadMapHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadMap8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.MAP_8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadMapHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadMap16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.MAP_16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadMapHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadMap32()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.MAP_32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadMapHeader();

                header.Should().Be(uint.MaxValue);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotMap()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadMapHeader());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }

        public class ReadListHeaderMethod
        {
            [Fact]
            public void ShouldReadTinyList()
            {
                var mockInput = IOExtensions.CreateMockStream(0x92);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadListHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadList8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.LIST_8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadListHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadList16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.LIST_16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadListHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadList32()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.LIST_32, PackStreamBitConverter.GetBytes((int) -1));
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadListHeader();

                header.Should().Be(uint.MaxValue);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotList()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadListHeader());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }

        public class ReadStructSignature
        {
            [Fact]
            public void ShouldCallReadByteOnce()
            {
                var mockInput = IOExtensions.CreateMockStream(0xFF);
                var reader = new PackStreamReader(mockInput.Object);

                var signature = reader.ReadStructSignature();

                signature.Should().Be(0xFF);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }
        }


        public class ReadStructHeaderMethod
        {
            [Fact]
            public void ShouldReadTinyStruct()
            {
                var mockInput = IOExtensions.CreateMockStream(0xB2);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadStructHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadStruct8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.STRUCT_8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadStructHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadStruct16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.STRUCT_16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object);

                var header = reader.ReadStructHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotStruct()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.FALSE);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.ReadStructHeader());

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
            [InlineData(PackStream.FLOAT_64, PackStream.PackType.Float)]
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
                var mockInput = IOExtensions.CreateMockStream(marker);
                var reader = new PackStreamReader(mockInput.Object);

                var nextType = reader.PeekNextType();

                nextType.Should().Be(expected);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteUnDefined()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.RESERVED_C4);
                var reader = new PackStreamReader(mockInput.Object);

                var ex = Xunit.Record.Exception(() => reader.PeekNextType());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }


    }
}
