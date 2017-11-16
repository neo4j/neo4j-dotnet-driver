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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
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
                var mockInput = IOExtensions.CreateMockStream(PackStream.Null);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadNull();

                real.Should().BeNull();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }


            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotNull()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes16);

                var unpacker = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => unpacker.ReadNull());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadBooleanMethod
        {
            [Fact]
            public void ShouldReadBooleanTrueSuccessfully()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.True);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBoolean();

                real.Should().BeTrue();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadBooleanFalseSuccessfully()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBoolean();

                real.Should().BeFalse();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }


            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotTrueOrFalse()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes16);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

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
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = (sbyte)reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadLongAsSignedByte()
            {
                const sbyte expected = 1;
                var mockInput = IOExtensions.CreateMockStream(PackStream.Int8, (byte) expected);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = (sbyte)reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsShort()
            {
                const short expected = 124;
                var mockInput = IOExtensions.CreateMockStream(PackStream.Int16, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsInt()
            {
                const int expected = 1024;
                var mockInput = IOExtensions.CreateMockStream(PackStream.Int32, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadLongAsLong()
            {
                const long expected = 1024;
                var mockInput = IOExtensions.CreateMockStream(PackStream.Int64, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadLong();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotLong()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes16);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

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
                var mockInput = IOExtensions.CreateMockStream(PackStream.Float64, PackStreamBitConverter.GetBytes(expected));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadDouble();

                real.Should().Be(expected);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotDouble()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes16);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadDouble());
                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class ReadStringMethod
        {
            [Fact]
            public void ShouldReadTinyStringAsEmptyString()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.TinyString);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadString();

                real.Should().BeEmpty(); //.Equals(String.Empty);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadStringLessThan16Chars()
            {
                var mockInput = IOExtensions.CreateMockStream(0x81, 0x61);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldReadString8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.String8, (byte) 1, 0x61);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadString16()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] {PackStream.String16},
                    PackStreamBitConverter.GetBytes((short) 1), new byte[] {0x61});
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadString32()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.String32 },
                    PackStreamBitConverter.GetBytes((int)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadString();

                real.Should().Be("a");
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldThrowExceptionWhenReadString32ReturnsStringSizeLonggerThanIntMax()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.String32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadString());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotString()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadString());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }
        }

        public class ReadByteMethod
        {

            [Fact]
            public void ShouldReadZeroLengthBytes()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes8, 0);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBytes();

                real.Length.Should().Be(0);
            }

            [Fact]
            public void ShouldReadBytes8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes8, 1, 0x61);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadBytes16()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.Bytes16 },
                    PackStreamBitConverter.GetBytes((short)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldReadBytes32()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[] { PackStream.Bytes32 },
                    PackStreamBitConverter.GetBytes((int)1), new byte[] { 0x61 });
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.ReadBytes();

                real.Length.Should().Be(1);
                real.Should().Contain(0x61);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            }

            [Fact]
            public void ShouldThrowExceptionWhenReadBytes32ReturnsBytesSizeLonggerThanIntMax()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Bytes32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadBytes());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotBytes()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

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
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadMapHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadMap8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Map8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadMapHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadMap16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Map16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadMapHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadMap32()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Map32, PackStreamBitConverter.GetBytes((int)-1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadMapHeader();

                header.Should().Be(uint.MaxValue);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotMap()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

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
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadListHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadList8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.List8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadListHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadList16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.List16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadListHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadList32()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.List32, PackStreamBitConverter.GetBytes((int) -1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadListHeader();

                header.Should().Be(uint.MaxValue);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotList()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadListHeader());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }

        public class ReadStructSignatureMethod
        {
            [Fact]
            public void ShouldCallReadByteOnce()
            {
                var mockInput = IOExtensions.CreateMockStream(0xFF);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

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
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();

                header.Should().Be(2);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldReadStruct8()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Struct8, (byte) 1);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldReadStruct16()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.Struct16, PackStreamBitConverter.GetBytes((short)1));
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();

                header.Should().Be(1);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteNotStruct()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.ReadStructHeader());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }

        public class PeekNextTypeMethod
        {
            [Theory]
            [InlineData(PackStream.TinyString | 0x08, PackStream.PackType.String)]
            [InlineData(PackStream.TinyList | 0x08, PackStream.PackType.List)]
            [InlineData(PackStream.TinyMap | 0x08, PackStream.PackType.Map)]
            [InlineData(PackStream.TinyStruct | 0x08, PackStream.PackType.Struct)]
            [InlineData(PackStream.Null, PackStream.PackType.Null)]
            [InlineData(PackStream.True, PackStream.PackType.Boolean)]
            [InlineData(PackStream.False, PackStream.PackType.Boolean)]
            [InlineData(PackStream.Float64, PackStream.PackType.Float)]
            [InlineData(PackStream.Bytes8, PackStream.PackType.Bytes)]
            [InlineData(PackStream.Bytes16, PackStream.PackType.Bytes)]
            [InlineData(PackStream.Bytes32, PackStream.PackType.Bytes)]
            [InlineData(PackStream.String8, PackStream.PackType.String)]
            [InlineData(PackStream.String16, PackStream.PackType.String)]
            [InlineData(PackStream.String32, PackStream.PackType.String)]
            [InlineData(PackStream.List8, PackStream.PackType.List)]
            [InlineData(PackStream.List16, PackStream.PackType.List)]
            [InlineData(PackStream.List32, PackStream.PackType.List)]
            [InlineData(PackStream.Map8, PackStream.PackType.Map)]
            [InlineData(PackStream.Map16, PackStream.PackType.Map)]
            [InlineData(PackStream.Map32, PackStream.PackType.Map)]
            [InlineData(PackStream.Struct8, PackStream.PackType.Struct)]
            [InlineData(PackStream.Struct16, PackStream.PackType.Struct)]
            [InlineData(PackStream.Int8, PackStream.PackType.Integer)]
            [InlineData(PackStream.Int16, PackStream.PackType.Integer)]
            [InlineData(PackStream.Int32, PackStream.PackType.Integer)]
            [InlineData(PackStream.Int64, PackStream.PackType.Integer)]
            internal void ShouldPeekTypeCorrectly(byte marker, PackStream.PackType expected)
            {
                var mockInput = IOExtensions.CreateMockStream(marker);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var nextType = reader.PeekNextType();

                nextType.Should().Be(expected);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfMarkerByteUnDefined()
            {
                var mockInput = IOExtensions.CreateMockStream(PackStream.ReservedC4);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Xunit.Record.Exception(() => reader.PeekNextType());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.ReadByte(), Times.Once);
            }
        }

        public class PeekByteMethod
        {

            [Fact]
            public void ShouldPeekCorrectly()
            {
                const byte expected = 1;
                var mockInput = IOExtensions.CreateMockStream(1, 2, 3);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.PeekByte();

                real.Should().Be(expected);
                mockInput.Verify(x => x.ReadByte(), Times.Once);
                mockInput.Verify(x => x.Length, Times.Once);
                mockInput.Verify(x => x.Position, Times.Once);
                mockInput.Verify(x => x.Seek(-1, SeekOrigin.Current), Times.Once);
            }


            [Fact]
            public void ShouldThrowExceptionWhenTheresNoBytesToPeek()
            {
                var mockInput = IOExtensions.CreateMockStream(new byte[0]);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var ex = Record.Exception(() => reader.PeekByte());

                ex.Should().BeOfType<ProtocolException>();
                mockInput.Verify(x => x.Length, Times.Once);
                mockInput.Verify(x => x.Position, Times.Once);
                mockInput.Verify(x => x.Seek(-1, SeekOrigin.Current), Times.Never);
            }

        }

        public class ReadMapMethod
        {

            [Fact]
            public void ShouldReadEmptyTinyMapCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("A0".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyMap8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D8 00".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyMap16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D9 00 00 ".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyMap32Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("DA 00 00 00 00".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadTinyMapCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream(
                        "AF 81 61  01 81 62 01  81 63 03 81  64 04 81 65 05 81 66 06  81 67 07 81  68 08 81 69  09 81 6A 00 81 6B 01 81  6C 02 81 6D  03 81 6E 04  81 6F 05"
                            .ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(15);
                map.Should().ContainKeys("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o");
                map.Should().ContainValues(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 0L);
            }

            [Fact]
            public void ShouldReadMap8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream(
                        "D8 10 81 61  01 81 62 01  81 63 03 81  64 04 81 65 05 81 66 06  81 67 07 81  68 08 81 69  09 81 6A 00 81 6B 01 81  6C 02 81 6D  03 81 6E 04  81 6F 05 81 70 06"
                            .ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(16);
                map.Should().ContainKeys("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
                    "p");
                map.Should().ContainValues(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 0L);
            }

            [Fact]
            public void ShouldReadMap16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream(
                        "D9 00 10 81 61  01 81 62 01  81 63 03 81  64 04 81 65 05 81 66 06  81 67 07 81  68 08 81 69  09 81 6A 00 81 6B 01 81  6C 02 81 6D  03 81 6E 04  81 6F 05 81 70 06"
                            .ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(16);
                map.Should().ContainKeys("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
                    "p");
                map.Should().ContainValues(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 0L);
            }

            [Fact]
            public void ShouldReadMap32Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream(
                        "DA 00 00 00 10 81 61  01 81 62 01  81 63 03 81  64 04 81 65 05 81 66 06  81 67 07 81  68 08 81 69  09 81 6A 00 81 6B 01 81  6C 02 81 6D  03 81 6E 04  81 6F 05 81 70 06"
                            .ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IDictionary<string, object>>();

                var map = real as IDictionary<string, object>;
                map.Should().NotBeNull();
                map.Count.Should().Be(16);
                map.Should().ContainKeys("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
                    "p");
                map.Should().ContainValues(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 0L);
            }

        }

        public class ReadStructMethod
        {

            [Fact]
            public void ShouldReadEmptyTinyStructCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("B0 01".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyStruct8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("DC 00 01".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyStruct16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("DD 00 00 01".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadTinyStructCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("B3 01 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(3);
                fields.Should().Contain(1L);
                fields.Should().Contain(2L);
                fields.Should().Contain(3L);
            }

            [Fact]
            public void ShouldReadStruct8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("DC 03 01 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(3);
                fields.Should().Contain(1L);
                fields.Should().Contain(2L);
                fields.Should().Contain(3L);
            }

            [Fact]
            public void ShouldReadStruct16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("DD 00 03 01 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var header = reader.ReadStructHeader();
                var signature = reader.ReadStructSignature();
                var fields = new List<object>();
                for (var i = 0; i < header; i++)
                {
                    fields.Add(reader.Read());
                }

                signature.Should().Be(0x01);
                fields.Count.Should().Be(3);
                fields.Should().Contain(1L);
                fields.Should().Contain(2L);
                fields.Should().Contain(3L);
            }
            
        }

        public class ReadListMethod
        {

            [Fact]
            public void ShouldReadEmptyTinyListCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("90".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyList8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D4 00".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyMap16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D5 00 00 ".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadEmptyMap32Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D6 00 00 00 00".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldReadTinyListCorrectly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("93 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(3);
                list.Should().Contain(1L);
                list.Should().Contain(2L);
                list.Should().Contain(3L);
            }

            [Fact]
            public void ShouldReadMap8Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D4 03 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(3);
                list.Should().Contain(1L);
                list.Should().Contain(2L);
                list.Should().Contain(3L);
            }

            [Fact]
            public void ShouldReadMap16Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D5 00 03 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(3);
                list.Should().Contain(1L);
                list.Should().Contain(2L);
                list.Should().Contain(3L);
            }

            [Fact]
            public void ShouldReadMap32Correctly()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D6 00 00 00 03 01 02 03".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeAssignableTo<IList<object>>();

                var list = real as IList<object>;
                list.Should().NotBeNull();
                list.Count.Should().Be(3);
                list.Should().Contain(1L);
                list.Should().Contain(2L);
                list.Should().Contain(3L);
            }

        }

        public class ReadMethod
        {

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void ShouldReadBooleanThroughRead(bool value)
            {
                var mockInput =
                    IOExtensions.CreateMockStream(value ? PackStream.True : PackStream.False);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeOfType<bool>();

                var boolean = (bool)real;
                boolean.Should().Be(value);
            }

            [Fact]
            public void ShouldReadNullThroughRead()
            {
                var mockInput =
                    IOExtensions.CreateMockStream(PackStream.Null);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeNull();
            }

            [Fact]
            public void ShouldReadBytesThroughRead()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("CC 01 01".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeOfType<byte[]>();

                var bytes = real as byte[];
                bytes.Should().NotBeNull();
                bytes.Length.Should().Be(1);
            }

            [Fact]
            public void ShouldReadFloatThroughRead()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("C1 3F F1 99 99 99 99 99 9A".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeOfType<double>();

                var number = (double)real;
                number.Should().Be(1.1);
            }

            [Fact]
            public void ShouldReadStringThroughRead()
            {
                var mockInput =
                    IOExtensions.CreateMockStream("D0 1A 61 62  63 64 65 66 67 68 69 6A 6B 6C 6D 6E 6F 70 71 72 73 74 75 76 77 78 79 7A".ToByteArray());
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();
                real.Should().BeOfType<string>();

                var text = (string)real;
                text.Should().Be("abcdefghijklmnopqrstuvwxyz");
            }
            
            [Theory]
            [InlineData(2147483648, new byte[] { 0xCB, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 })]
            [InlineData(9223372036854775807, new byte[] { 0xCB, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
            public void ReadLongCorrectly(long expected, byte[] data)
            {
                ShouldReadNumCorrectly(expected, data);
            }

            [Theory]
            [InlineData(32768, new byte[] { 0xCA, 0x00, 0x00, 0x80, 0x00 })]
            public void ReadIntCorrectly(int expected, byte[] data)
            {
                ShouldReadNumCorrectly(expected, data);
            }

            [Theory]
            [InlineData(-16, new byte[] { 0xF0 })]
            [InlineData(42, new byte[] { 0x2A })]
            [InlineData(127, new byte[] { 0x7F })]
            public void ReadTinyIntCorrectly(sbyte value, byte[] expected)
            {
                ShouldReadNumCorrectly(value, expected);
            }

            [Theory]
            [InlineData(-128, new byte[] { 0xC8, 0x80 })]
            [InlineData(-17, new byte[] { 0xC8, 0xEF })]
            public void ReadInt8Correctly(sbyte value, byte[] expected)
            {
                ShouldReadNumCorrectly(value, expected);
            }

            [Theory]
            [InlineData(128, new byte[] { 0xC9, 0x00, 0x80 })]
            public void ReadShortCorrectly(short value, byte[] expected)
            {
                ShouldReadNumCorrectly(value, expected);
            }

            private void ShouldReadNumCorrectly(long expected, byte[] data)
            {
                var mockInput = IOExtensions.CreateMockStream(data);
                var reader = new PackStreamReader(mockInput.Object, BoltReader.StructHandlers);

                var real = reader.Read();

                real.Should().Be(expected);
            }

        }

        public class ReadValueMethod
        {

            [Fact]
            public void ShouldThrowWhenPackTypeIsNotSupported()
            {
                var reader = new PackStreamReader(new MemoryStream(), BoltReader.StructHandlers);

                var ex = Record.Exception(() => reader.ReadValue((PackStream.PackType)100));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

        }

    }
}
