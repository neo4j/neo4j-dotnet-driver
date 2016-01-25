using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Sockets.Plugin.Abstractions;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public partial class PackStreamTests
    {

        public class PackerTests
        {
            private class Mocks
            {
                public Mock<IOutputStream> MockOutputStream { get; }
                public IOutputStream OutputStream => MockOutputStream.Object;

                public string Received { get; set; }
                public string ReceivedByteArray { get; set; }

                public Mocks()
                {
                    MockOutputStream = new Mock<IOutputStream>();

                    MockOutputStream
                        .Setup(s => s.Write(It.IsAny<byte>(), It.IsAny<byte[]>()))
                        .Callback<byte, byte[]>((b, bArray) => Received = $"{b.ToString("X2")} {bArray.ToHexString(0)}")
                        .Returns(OutputStream);

                    MockOutputStream
                        .Setup(s => s.Write(It.IsAny<byte[]>()))
                        .Callback<byte[]>((bArray) => ReceivedByteArray = $"{bArray.ToHexString(0)}")
                        .Returns(OutputStream);
                }

                public void VerifyWrite(byte b, params byte[] bytes)
                {
                    byte[] expectedBytes = bytes;
                    MockOutputStream.Verify(c => c.Write(b, bytes ?? It.IsAny<byte[]>()), Times.Once,
                        $"Received {Received}{Environment.NewLine}Expected {b.ToString("X2")} {expectedBytes.ToHexString(0)}");
                }

                public void VerifyWrite(byte[] bytes)
                {
                    byte[] expectedBytes = bytes;
                    MockOutputStream.Verify(c => c.Write(bytes ?? It.IsAny<byte[]>()), Times.Once,
                        $"Received {ReceivedByteArray}{Environment.NewLine}Expected {expectedBytes.ToHexString(0)}");
                }
            }

            public class PackNullMethod
            {
                [Fact]
                public void ShouldPackNullSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.PackNull();
                    mocks.VerifyWrite(PackStream.NULL);
                }


            }

//            public class PackRawMethod
//            {
//                [Fact]
//                public void ShouldUnpacPawBytesSuccessfully()
//                {
//                    var mocks = new Mocks();
//                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
//
//                    var bytes = new byte[] { 1, 2, 3 };
//                    u.PackRaw(bytes);
//                    mocks.VerifyWrite(bytes);
//                }
//            }

            public class PackLongMethod
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
                public void ShouldPackLongSuccessfully(long input, byte marker, string expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(input);
                    mocks.VerifyWrite(marker);
                    if (expected != null)
                    {
                        mocks.VerifyWrite(expected.ToByteArray());
                    }
                }
            }

            public class PackDoubleMethod
            {
                [Theory]
                [InlineData(1.2, "3F F3 33 33 33 33 33 33")]
                public void ShouldPackDoubleSuccessfully(double input, string expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(input);
                    mocks.VerifyWrite(PackStream.FLOAT_64);
                    mocks.VerifyWrite(expected.ToByteArray());
                }
            }

            public class PackBoolMethod
            {
                [Theory]
                [InlineData(true)]
                [InlineData(false)]
                public void ShouldPackBoolSuccessfully(bool input)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(input);
                    if (input)
                    {
                        mocks.VerifyWrite(PackStream.TRUE);

                    }
                    else
                    {
                        mocks.VerifyWrite(PackStream.FALSE);
                    }
                }
            }

            public class PackStringMethod
            {
                [Fact]
                public void ShouldPackNullStringSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack((string) null);
                    mocks.VerifyWrite(PackStream.NULL);

                }

                [Fact]
                public void ShouldPackEmptyStringSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(string.Empty);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 0);

                }

                [Theory]
                [InlineData(20, PackStream.STRING_8, new byte[] {20})]
                [InlineData(byte.MaxValue + 1, PackStream.STRING_16, new byte[] {0x01, 0x00})]
                [InlineData(short.MaxValue + 1, PackStream.STRING_32, new byte[] {0x00, 0x00, 0x80, 0x00})]
                public void ShouldPackStringSuccessfully(int size, byte marker, byte[] sizeByte)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    var input = new string('a', size);
                    var expected = new byte[size];
                    for (int i = 0; i < size; i ++)
                    {
                        expected[i] = 97;
                    }

                    u.Pack(input);

                    mocks.VerifyWrite(marker, sizeByte);
                    mocks.VerifyWrite(expected);
                }

                //packStringUniCodeCorrectly
                [Theory]
                [InlineData(20, PackStream.STRING_8, new byte[] {0x28})]
                public void ShouldPackUnicodeStringSuccessfully(int size, byte marker, byte[] sizeByte)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    var input = new string('å', size);
                    var expected = new byte[size*2];
                    for (int i = 0; i < size*2; i += 2)
                    {
                        expected[i] = 0xC3;
                        expected[i + 1] = 0xA5;
                    }

                    u.Pack(input);

                    mocks.VerifyWrite(marker, sizeByte);
                    mocks.VerifyWrite(expected);
                }
            }

            public class PackBytesMethod
            {
                [Fact]
                public void ShouldPackNullBytesSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack((byte[]) null);
                    mocks.VerifyWrite(PackStream.NULL);

                }

                [Fact]
                public void ShouldPackEmptyByteSuccessfully()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    u.Pack(new byte[] {});
                    mocks.VerifyWrite(PackStream.BYTES_8, new byte[] {0});

                }

                [Theory]
                [InlineData(20, PackStream.BYTES_8, new byte[] {20})]
                [InlineData(byte.MaxValue + 1, PackStream.BYTES_16, new byte[] {0x01, 0x00})]
                [InlineData(short.MaxValue + 1, PackStream.BYTES_32, new byte[] {0x00, 0x00, 0x80, 0x00})]
                public void ShouldPackStringSuccessfully(int size, byte marker, byte[] sizeByte)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    var expected = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        expected[i] = 97;
                    }

                    u.Pack(expected);

                    mocks.VerifyWrite(marker, sizeByte);
                    mocks.VerifyWrite(expected);
                }
            }

            public class PackObjectMethod
            {
                [Fact]
                public void ShouldPackAsNull()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack((object) null);

                    mocks.VerifyWrite(PackStream.NULL);
                }

                [Theory]
                [InlineData(true, PackStream.TRUE)]
                [InlineData(null, PackStream.NULL)]
                public void ShouldPackNullableBool(bool? input, byte expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack(input);
                    mocks.VerifyWrite(expected);
                }

                [Theory]
                [InlineData((sbyte) -128, PackStream.INT_8)]
                [InlineData(null, PackStream.NULL)]
                public void ShouldPackNullableAsNull(sbyte? input, byte expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack(input);
                    mocks.VerifyWrite(expected);
                }

                [Theory]
                [InlineData((byte) 123, (byte) 123)]
                [InlineData(-128, PackStream.INT_8)]
                [InlineData(short.MaxValue, PackStream.INT_16)]
                [InlineData(short.MinValue, PackStream.INT_16)]
                [InlineData(int.MaxValue, PackStream.INT_32)]
                [InlineData(int.MinValue, PackStream.INT_32)]
                [InlineData(long.MaxValue, PackStream.INT_64)]
                [InlineData(long.MinValue, PackStream.INT_64)]
                public void ShouldPackNumbersAsLong(object input, byte expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack(input);
                    mocks.VerifyWrite(expected);
                }


                [Theory]
                [InlineData((float) 123.0, PackStream.FLOAT_64)]
                [InlineData((double) 123.0, PackStream.FLOAT_64)]
                public void ShouldPackFloatNumbersAsDouble(object input, byte expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack(input);
                    mocks.VerifyWrite(expected);
                }

                [Fact]
                public void ShouldPackDecimalNumbersAsDouble()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    decimal input = 1.34m;
                    u.Pack((object) input);
                    mocks.VerifyWrite(PackStream.FLOAT_64);
                }


                [Fact]
                public void ShouldPackAsByteArray()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    var input = new byte[] {1, 2, 3};
                    u.Pack((object) input);
                    mocks.VerifyWrite(PackStream.BYTES_8, 3);
                }

                [Fact]
                public void ShouldPackCharAsString()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    char input = 'a';
                    u.Pack((object) input);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                }

                [Fact]
                public void ShouldPackAsString()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    string input = "abc";
                    u.Pack((object) input);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 3);
                }

                [Fact]
                public void ShouldPackAsList()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    IList<object> list = new List<object>();
                    list.Add(1);
                    list.Add(true);
                    list.Add("a");
                    u.Pack((object) list);

                    mocks.VerifyWrite((byte) (PackStream.TINY_LIST | list.Count));
                    mocks.VerifyWrite((byte) 1);
                    mocks.VerifyWrite(PackStream.TRUE);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                    mocks.VerifyWrite(new byte[] {97});
                }

                [Fact]
                public void ShouldPackArrayAsList()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    int[] list = new int[2];
                    list[0]=1;
                    list[1]=2;
                    u.Pack((object)list);

                    mocks.VerifyWrite((byte)(PackStream.TINY_LIST | list.Length));
                    mocks.VerifyWrite((byte)1);
                    mocks.VerifyWrite((byte)2);
                }

                //
                [Fact]
                public void ShouldPackAsDictionary()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    IDictionary<object, object> dic = new Dictionary<object, object>();
                    dic.Add(true, "a");
                    u.Pack(dic);

                    mocks.VerifyWrite((byte) (PackStream.TINY_MAP | dic.Count));
                    mocks.VerifyWrite(PackStream.TRUE);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                    mocks.VerifyWrite(new byte[] {97});
                }

                // throw exception
                [Fact]
                public void ShouldThrowExceptionIfTypeUnknown()
                {
                    var packer = new PackStream.Packer(null, null);
                    var ex = Xunit.Record.Exception(() => packer.Pack(new {Name = "Test"}));
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                }
            }

            public class PackListMethod
            {
                [Fact]
                public void ShouldPackAsNullIfListIsNull()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack((IList) null);

                    mocks.VerifyWrite(PackStream.NULL);
                }

                [Theory]
                [InlineData(0x0F, PackStream.TINY_LIST | 0x0F, new byte[0])]
                [InlineData(byte.MaxValue, PackStream.LIST_8, new[] {byte.MaxValue})]
                [InlineData(short.MaxValue, PackStream.LIST_16, new byte[] {0x7F, 0xFF})]
                [InlineData(int.MaxValue, PackStream.LIST_32, new byte[] {0x7F, 0xFF, 0xFF, 0xFF})]
                public void ShouldPackListHeaderCorrectly(int size, byte marker, byte[] expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.PackListHeader(size);

                    mocks.VerifyWrite(marker, expected);
                }

                [Fact]
                public void ShouldPackListOfDifferentTypeCorrectly()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    var list = new List<object>();
                    list.Add(1);
                    list.Add(true);
                    list.Add("a");
                    u.Pack((IList) list);

                    mocks.VerifyWrite((byte) (PackStream.TINY_LIST | list.Count));
                    mocks.VerifyWrite((byte) 1);
                    mocks.VerifyWrite(PackStream.TRUE);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                    mocks.VerifyWrite(new byte[] {97});
                }
            }

            public class PackDictionaryMethod
            {
                [Fact]
                public void ShouldPackAsNullIfDictionaryIsNull()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.Pack((IDictionary) null);

                    mocks.VerifyWrite(PackStream.NULL);
                }

                [Theory]
                [InlineData(0x0F, PackStream.TINY_MAP | 0x0F, new byte[0])]
                [InlineData(byte.MaxValue, PackStream.MAP_8, new[] {byte.MaxValue})]
                [InlineData(short.MaxValue, PackStream.MAP_16, new byte[] {0x7F, 0xFF})]
                [InlineData(int.MaxValue, PackStream.MAP_32, new byte[] {0x7F, 0xFF, 0xFF, 0xFF})]
                public void ShouldPackListHeaderCorrectly(int size, byte marker, byte[] expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.PackMapHeader(size);

                    mocks.VerifyWrite(marker, expected);
                }

                [Fact]
                public void ShouldPackMapOfDifferentTypeCorrectly()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());

                    IDictionary<object, object> dic = new Dictionary<object, object>();
                    dic.Add(true, "a");
                    u.Pack((IDictionary) dic);

                    mocks.VerifyWrite((byte) (PackStream.TINY_MAP | dic.Count));
                    mocks.VerifyWrite(PackStream.TRUE);
                    mocks.VerifyWrite(PackStream.TINY_STRING | 1);
                    mocks.VerifyWrite(new byte[] {97});
                }
            }

            public class PackStructHeaderMethod
            {

                [Theory]
                [InlineData(0x0F, PackStream.TINY_STRUCT | 0x0F, new byte[] { 0x77})]
                [InlineData(byte.MaxValue, PackStream.STRUCT_8, new byte[] { byte.MaxValue, 0x77})]
                public void ShouldPackStructHeaderCorrectly(int size, byte marker, byte[] expected)
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.PackStructHeader(size, 0x77);

                    mocks.VerifyWrite(marker, expected);
                }

                [Fact]
                public void ShouldPackStructHeaderStruct16Correctly()
                {
                    var mocks = new Mocks();
                    var u = new PackStream.Packer(mocks.OutputStream, new BigEndianTargetBitConverter());
                    u.PackStructHeader(short.MaxValue, 0x77);

                    mocks.VerifyWrite(PackStream.STRUCT_16, new byte[] { 0x7F, 0xFF });
                    mocks.VerifyWrite(0x77);
                }

                [Fact]
                public void ShouldThrowExceptionIfSizeIsGreaterThanShortMax()
                {
                    var packer = new PackStream.Packer(null, null);
                    var ex = Xunit.Record.Exception(() => packer.PackStructHeader(short.MaxValue +1, 0x1));
                    ex.Should().BeOfType<ArgumentOutOfRangeException>();
                }
            }

        }
    }
}
