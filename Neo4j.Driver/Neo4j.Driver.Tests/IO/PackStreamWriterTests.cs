using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class PackStreamWriterTests
    {

        private class Mocks
        {
            private readonly Mock<Stream> _mockOutputStream;
            private readonly Queue<string> _receviedBytes = new Queue<string>();
            private readonly Queue<string> _receivedByteArrays = new Queue<string>();

            public Mocks()
            {
                _mockOutputStream = new Mock<Stream>();
                _mockOutputStream.Setup(s => s.CanWrite).Returns(true);
                _mockOutputStream
                    .Setup(s => s.WriteByte(It.IsAny<byte>()))
                    .Callback<byte>(b => _receviedBytes.Enqueue($"{b:X2}"));
                _mockOutputStream
                    .Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback<byte[], int, int>((bArray, offset, count) => _receivedByteArrays.Enqueue($"{bArray.ToHexString(offset, count)}"));
            }

            public Stream OutputStream => _mockOutputStream.Object;

            public void VerifyWrite(byte expectedByte)
            {
                _mockOutputStream.Verify(c => c.WriteByte(expectedByte), Times.Once,
                    $"Received {_receviedBytes.Dequeue()}{Environment.NewLine}Expected {expectedByte:X2}");
            }

            public void VerifyWrite(byte[] expectedBytes)
            {
                _mockOutputStream.Verify(c => c.Write(expectedBytes, It.IsAny<int>(), It.IsAny<int>()), Times.Once,
                    $"Received {_receivedByteArrays.Dequeue()}{Environment.NewLine}Expected {expectedBytes.ToHexString()}");
            }
        }


        public class WriteNullMethod
        {
            [Fact]
            public void ShouldWriteNullSuccessfully()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteNull();

                mocks.VerifyWrite(PackStream.NULL);
            }


        }

        public class WriteLongMethod
        {
            [Theory]
            [InlineData(PackStream.MINUS_2_TO_THE_4, 0xF0, null)]
            [InlineData(PackStream.PLUS_2_TO_THE_7 - 1, 0x7F, null)]
            [InlineData(PackStream.MINUS_2_TO_THE_7, PackStream.INT_8, "80")]
            [InlineData(PackStream.MINUS_2_TO_THE_4 - 1, PackStream.INT_8, "EF")]
            [InlineData(PackStream.MINUS_2_TO_THE_15, PackStream.INT_16, "80 00")]
            [InlineData(PackStream.PLUS_2_TO_THE_15 - 1, PackStream.INT_16, "7F FF")]
            [InlineData(PackStream.MINUS_2_TO_THE_31, PackStream.INT_32, "80 00 00 00")]
            [InlineData(PackStream.PLUS_2_TO_THE_31 - 1, PackStream.INT_32, "7F FF FF FF")]
            [InlineData(long.MinValue, PackStream.INT_64, "80 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, PackStream.INT_64, "7F FF FF FF FF FF FF FF")]
            public void ShouldWriteLongSuccessfully(long input, byte marker, string expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(marker);
                if (expected != null)
                {
                    mocks.VerifyWrite(expected.ToByteArray());
                }
            }
        }

        public class WriteDoubleMethod
        {
            [Theory]
            [InlineData(1.2, "3F F3 33 33 33 33 33 33")]
            public void ShouldWriteDoubleSuccessfully(double input, string expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(PackStream.FLOAT_64);
                mocks.VerifyWrite(expected.ToByteArray());
            }
        }

        public class WriteBoolMethod
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void ShouldWriteBoolSuccessfully(bool input)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(input ? PackStream.TRUE : PackStream.FALSE);
            }
        }

        public class WriteStringMethod
        {
            [Fact]
            public void ShouldWriteNullStringSuccessfully()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((string)null);

                mocks.VerifyWrite(PackStream.NULL);
            }

            [Fact]
            public void ShouldWriteEmptyStringSuccessfully()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(string.Empty);

                mocks.VerifyWrite(PackStream.TINY_STRING | 0);
            }

            [Theory]
            [InlineData(20, PackStream.STRING_8, new byte[] { 20 })]
            [InlineData(byte.MaxValue + 1, PackStream.STRING_16, new byte[] { 0x01, 0x00 })]
            [InlineData(short.MaxValue + 1, PackStream.STRING_32, new byte[] { 0x00, 0x00, 0x80, 0x00 })]
            public void ShouldWriteStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var input = new string('a', size);
                var expected = new byte[size];
                for (var i = 0; i < size; i++)
                {
                    expected[i] = 97;
                }

                writer.Write(input);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(sizeByte);
                mocks.VerifyWrite(expected);
            }

            //packStringUniCodeCorrectly
            [Theory]
            [InlineData(20, PackStream.STRING_8, new byte[] { 0x28 })]
            public void ShouldWriteUnicodeStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var input = new string('å', size);
                var expected = new byte[size * 2];
                for (var i = 0; i < size * 2; i += 2)
                {
                    expected[i] = 0xC3;
                    expected[i + 1] = 0xA5;
                }

                writer.Write(input);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(sizeByte);
                mocks.VerifyWrite(expected);
            }
        }

        public class WriteBytesMethod
        {
            [Fact]
            public void ShouldWriteNullBytesSuccessfully()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((byte[])null);

                mocks.VerifyWrite(PackStream.NULL);
            }

            [Fact]
            public void ShouldWriteEmptyByteSuccessfully()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(new byte[] { });

                mocks.VerifyWrite(PackStream.BYTES_8);
                mocks.VerifyWrite(new byte[] { 0 });

            }

            [Theory]
            [InlineData(20, PackStream.BYTES_8, new byte[] { 20 })]
            [InlineData(byte.MaxValue + 1, PackStream.BYTES_16, new byte[] { 0x01, 0x00 })]
            [InlineData(short.MaxValue + 1, PackStream.BYTES_32, new byte[] { 0x00, 0x00, 0x80, 0x00 })]
            public void ShouldWriteStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var expected = new byte[size];
                for (int i = 0; i < size; i++)
                {
                    expected[i] = 97;
                }

                writer.Write(expected);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(sizeByte);
                mocks.VerifyWrite(expected);
            }
        }

        public class WriteObjectMethod
        {
            [Fact]
            public void ShouldWriteAsNull()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)null);

                mocks.VerifyWrite(PackStream.NULL);
            }

            [Theory]
            [InlineData(true, PackStream.TRUE)]
            [InlineData(null, PackStream.NULL)]
            public void ShouldWriteNullableBool(bool? input, byte expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Theory]
            [InlineData((sbyte)-128, PackStream.INT_8)]
            [InlineData(null, PackStream.NULL)]
            public void ShouldWriteNullableAsNull(sbyte? input, byte expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Theory]
            [InlineData((byte)123, (byte)123)]
            [InlineData(-128, PackStream.INT_8)]
            [InlineData(short.MaxValue, PackStream.INT_16)]
            [InlineData(short.MinValue, PackStream.INT_16)]
            [InlineData(int.MaxValue, PackStream.INT_32)]
            [InlineData(int.MinValue, PackStream.INT_32)]
            [InlineData(long.MaxValue, PackStream.INT_64)]
            [InlineData(long.MinValue, PackStream.INT_64)]
            public void ShouldWriteNumbersAsLong(object input, byte expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }


            [Theory]
            [InlineData((float)123.0, PackStream.FLOAT_64)]
            [InlineData(123.0, PackStream.FLOAT_64)]
            public void ShouldWriteFloatNumbersAsDouble(object input, byte expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteDecimalNumbersAsDouble()
            {
                object input = (double)1.34m;

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(PackStream.FLOAT_64);
            }


            [Fact]
            public void ShouldWriteAsByteArray()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);
                var input = new byte[] { 1, 2, 3 };

                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.BYTES_8);
                mocks.VerifyWrite(new byte[] { 3 });
            }

            [Fact]
            public void ShouldWriteCharAsString()
            {
                const char input = 'a';

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.TINY_STRING | 1);
            }

            [Fact]
            public void ShouldWriteAsString()
            {
                const string input = "abc";

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);
                
                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.TINY_STRING | 3);
            }

            [Fact]
            public void ShouldWriteAsList()
            {
                var list = new List<object>(new object[] {1, true, "a"});

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)list);

                mocks.VerifyWrite((byte)(PackStream.TINY_LIST | list.Count));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(PackStream.TRUE);
                mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }

            [Fact]
            public void ShouldWriteArrayAsList()
            {
                var list = new[] {1, 2};

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)list);

                mocks.VerifyWrite((byte)(PackStream.TINY_LIST | list.Length));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(2);
            }

            //
            [Fact]
            public void ShouldWriteAsDictionary()
            {
                var dict = new Dictionary<object, object>() {{true, "a"}};

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object) dict);

                mocks.VerifyWrite((byte)(PackStream.TINY_MAP | dict.Count));
                mocks.VerifyWrite(PackStream.TRUE);
                mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }

            // throw exception
            [Fact]
            public void ShouldThrowExceptionIfTypeUnknown()
            {
                var writer = new PackStreamWriter(new MemoryStream());

                var ex = Record.Exception(() => writer.Write(new { Name = "Test" }));

                ex.Should().BeOfType<ProtocolException>();
            }
        }

        public class WriteListMethod
        {
            [Fact]
            public void ShouldWriteAsNullIfListIsNull()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((IList)null);

                mocks.VerifyWrite(PackStream.NULL);
            }

            [Theory]
            [InlineData(0x0F, PackStream.TINY_LIST | 0x0F, new byte[0])]
            [InlineData(byte.MaxValue, PackStream.LIST_8, new[] { byte.MaxValue })]
            [InlineData(short.MaxValue, PackStream.LIST_16, new byte[] { 0x7F, 0xFF })]
            [InlineData(int.MaxValue, PackStream.LIST_32, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
            public void ShouldWriteListHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteListHeader(size);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteListOfDifferentTypeCorrectly()
            {
                var list = new List<object>(new object[] {1, true, "a"});

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(list);

                mocks.VerifyWrite((byte)(PackStream.TINY_LIST | list.Count));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(PackStream.TRUE);
                mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }
        }

        public class WriteDictionaryMethod
        {
            [Fact]
            public void ShouldWriteAsNullIfDictionaryIsNull()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((IDictionary)null);

                mocks.VerifyWrite(PackStream.NULL);
            }

            [Theory]
            [InlineData(0x0F, PackStream.TINY_MAP | 0x0F, new byte[0])]
            [InlineData(byte.MaxValue, PackStream.MAP_8, new[] { byte.MaxValue })]
            [InlineData(short.MaxValue, PackStream.MAP_16, new byte[] { 0x7F, 0xFF })]
            [InlineData(int.MaxValue, PackStream.MAP_32, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
            public void ShouldWriteListHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteMapHeader(size);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteMapOfDifferentTypeCorrectly()
            {
                var dict = new Dictionary<object, object>() { { true, "a" } };

                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(dict);

                mocks.VerifyWrite((byte)(PackStream.TINY_MAP | dict.Count));
                mocks.VerifyWrite(PackStream.TRUE);
                mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }
        }

        public class WriteStructHeaderMethod
        {

            [Theory]
            [InlineData(0x0F, PackStream.TINY_STRUCT | 0x0F, new byte[] { 0x77 })]
            [InlineData(byte.MaxValue, PackStream.STRUCT_8, new byte[] { byte.MaxValue, 0x77 })]
            public void ShouldWriteStructHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteStructHeader(size, 0x77);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteStructHeaderStruct16Correctly()
            {
                var mocks = new Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteStructHeader(short.MaxValue, 0x77);

                mocks.VerifyWrite(PackStream.STRUCT_16);
                mocks.VerifyWrite(new byte[] { 0x7F, 0xFF });
                mocks.VerifyWrite(0x77);
            }

            [Fact]
            public void ShouldThrowExceptionIfSizeIsGreaterThanShortMax()
            {
                var packer = new PackStreamWriter(new MemoryStream());

                var ex = Record.Exception(() => packer.WriteStructHeader(short.MaxValue + 1, 0x1));

                ex.Should().BeOfType<ProtocolException>();
            }
        }

    }
}
