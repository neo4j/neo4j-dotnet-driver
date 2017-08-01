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
        
        public class WriteNullMethod
        {
            [Fact]
            public void ShouldWriteNullSuccessfully()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteNull();

                mocks.VerifyWrite(PackStream.Null);
            }


        }

        public class WriteLongMethod
        {
            [Theory]
            [InlineData(PackStream.Minus2ToThe4, 0xF0, null)]
            [InlineData(PackStream.Plus2ToThe7 - 1, 0x7F, null)]
            [InlineData(PackStream.Minus2ToThe7, PackStream.Int8, "80")]
            [InlineData(PackStream.Minus2ToThe4 - 1, PackStream.Int8, "EF")]
            [InlineData(PackStream.Minus2ToThe15, PackStream.Int16, "80 00")]
            [InlineData(PackStream.Plus2ToThe15 - 1, PackStream.Int16, "7F FF")]
            [InlineData(PackStream.Minus2ToThe31, PackStream.Int32, "80 00 00 00")]
            [InlineData(PackStream.Plus2ToThe31 - 1, PackStream.Int32, "7F FF FF FF")]
            [InlineData(long.MinValue, PackStream.Int64, "80 00 00 00 00 00 00 00")]
            [InlineData(long.MaxValue, PackStream.Int64, "7F FF FF FF FF FF FF FF")]
            public void ShouldWriteLongSuccessfully(long input, byte marker, string expected)
            {
                var mocks = new WriterTests.Mocks();
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
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(PackStream.Float64);
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
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(input ? PackStream.True : PackStream.False);
            }
        }

        public class WriteStringMethod
        {
            [Fact]
            public void ShouldWriteNullStringSuccessfully()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((string)null);

                mocks.VerifyWrite(PackStream.Null);
            }

            [Fact]
            public void ShouldWriteEmptyStringSuccessfully()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(string.Empty);

                mocks.VerifyWrite(PackStream.TinyString | 0);
            }

            [Theory]
            [InlineData(20, PackStream.String8, new byte[] { 20 })]
            [InlineData(byte.MaxValue + 1, PackStream.String16, new byte[] { 0x01, 0x00 })]
            [InlineData(short.MaxValue + 1, PackStream.String32, new byte[] { 0x00, 0x00, 0x80, 0x00 })]
            public void ShouldWriteStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new WriterTests.Mocks();
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
            [InlineData(20, PackStream.String8, new byte[] { 0x28 })]
            public void ShouldWriteUnicodeStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new WriterTests.Mocks();
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
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((byte[])null);

                mocks.VerifyWrite(PackStream.Null);
            }

            [Fact]
            public void ShouldWriteEmptyByteSuccessfully()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(new byte[] { });

                mocks.VerifyWrite(PackStream.Bytes8);
                mocks.VerifyWrite(new byte[] { 0 });

            }

            [Theory]
            [InlineData(20, PackStream.Bytes8, new byte[] { 20 })]
            [InlineData(byte.MaxValue + 1, PackStream.Bytes16, new byte[] { 0x01, 0x00 })]
            [InlineData(short.MaxValue + 1, PackStream.Bytes32, new byte[] { 0x00, 0x00, 0x80, 0x00 })]
            public void ShouldWriteStringSuccessfully(int size, byte marker, byte[] sizeByte)
            {
                var mocks = new WriterTests.Mocks();
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

        public class WriteStructMethod
        {

            [Theory]
            [InlineData(0x50, 0, 0xB0)]
            [InlineData(0x50, 1, 0xB1)]
            [InlineData(0x50, 10, 0xBA)]
            [InlineData(0x50, 15, 0xBF)]
            public void ShouldWriteTinyStructSuccessfully(byte signature, int fieldCount, byte expectedHeader)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var fields = new List<object>();
                for (var i = 0; i < fieldCount; i++)
                {
                    fields.Add(null);
                }

                var value = new PackStreamStruct(signature, fields);

                writer.Write(value);

                mocks.VerifyWrite(expectedHeader);
                mocks.VerifyWrite(new byte[] {signature});
            }

            [Theory]
            [InlineData(0x50, 16, PackStream.Struct8)]
            [InlineData(0x50, 40, PackStream.Struct8)]
            [InlineData(0x50, 255, PackStream.Struct8)]
            public void ShouldWriteStruct8Successfully(byte signature, int fieldCount, byte expectedHeader)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var fields = new List<object>();
                for (var i = 0; i < fieldCount; i++)
                {
                    fields.Add(null);
                }
                var value = new PackStreamStruct(signature, fields);

                writer.Write(value);

                mocks.VerifyWrite(expectedHeader);
                mocks.VerifyWrite(new byte[] { (byte)fieldCount, signature });
            }

            [Theory]
            [InlineData(0x50, 256, PackStream.Struct16)]
            [InlineData(0x50, 1000, PackStream.Struct16)]
            [InlineData(0x50, 32700, PackStream.Struct16)]
            public void ShouldWriteStruct16Successfully(byte signature, int fieldCount, byte expectedHeader)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var fields = new List<object>();
                for (var i = 0; i < fieldCount; i++)
                {
                    fields.Add(null);
                }
                var value = new PackStreamStruct(signature, fields);

                writer.Write(value);

                mocks.VerifyWrite(expectedHeader);
                mocks.VerifyWrite(PackStreamBitConverter.GetBytes((short)fieldCount));
                mocks.VerifyWrite(signature);
            }

            [Fact]
            public void ShouldWriteStructThroughWriteObject()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var value = new PackStreamStruct(0x50, Enumerable.Empty<object>());

                writer.Write((object)value);

                mocks.VerifyWrite(0xB0);
                mocks.VerifyWrite(new byte[] { 0x50});
            }

            [Fact]
            public void ShouldWriteNullStructAsNull()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((PackStreamStruct)null);

                mocks.VerifyWrite(0xC0);
            }

            [Fact]
            public void ShouldThrowExceptionWhenFieldCountExceedsMaximum()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                var fields = new List<object>();
                for (var i = 0; i < 35000; i++)
                {
                    fields.Add(0.0);
                }
                var value = new PackStreamStruct(0x50, fields);

                var ex = Record.Exception(() => writer.Write(value));

                ex.Should().BeOfType<ProtocolException>();
            }

        }

        public class WriteObjectMethod
        {
            [Fact]
            public void ShouldWriteAsNull()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)null);

                mocks.VerifyWrite(PackStream.Null);
            }

            [Theory]
            [InlineData(true, PackStream.True)]
            [InlineData(null, PackStream.Null)]
            public void ShouldWriteNullableBool(bool? input, byte expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Theory]
            [InlineData((sbyte)-128, PackStream.Int8)]
            [InlineData(null, PackStream.Null)]
            public void ShouldWriteNullableAsNull(sbyte? input, byte expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Theory]
            [InlineData((byte)123, (byte)123)]
            [InlineData(-128, PackStream.Int8)]
            [InlineData(short.MaxValue, PackStream.Int16)]
            [InlineData(short.MinValue, PackStream.Int16)]
            [InlineData(int.MaxValue, PackStream.Int32)]
            [InlineData(int.MinValue, PackStream.Int32)]
            [InlineData(long.MaxValue, PackStream.Int64)]
            [InlineData(long.MinValue, PackStream.Int64)]
            public void ShouldWriteNumbersAsLong(object input, byte expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }


            [Theory]
            [InlineData((float)123.0, PackStream.Float64)]
            [InlineData(123.0, PackStream.Float64)]
            public void ShouldWriteFloatNumbersAsDouble(object input, byte expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteDecimalNumbersAsDouble()
            {
                object input = (double)1.34m;

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(input);

                mocks.VerifyWrite(PackStream.Float64);
            }


            [Fact]
            public void ShouldWriteAsByteArray()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);
                var input = new byte[] { 1, 2, 3 };

                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.Bytes8);
                mocks.VerifyWrite(new byte[] { 3 });
            }

            [Fact]
            public void ShouldWriteCharAsString()
            {
                const char input = 'a';

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.TinyString | 1);
            }

            [Fact]
            public void ShouldWriteAsString()
            {
                const string input = "abc";

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);
                
                writer.Write((object)input);

                mocks.VerifyWrite(PackStream.TinyString | 3);
            }

            [Fact]
            public void ShouldWriteAsList()
            {
                var list = new List<object>(new object[] {1, true, "a"});

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)list);

                mocks.VerifyWrite((byte)(PackStream.TinyList | list.Count));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(PackStream.True);
                mocks.VerifyWrite(PackStream.TinyString | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }

            [Fact]
            public void ShouldWriteArrayAsList()
            {
                var list = new[] {1, 2};

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object)list);

                mocks.VerifyWrite((byte)(PackStream.TinyList | list.Length));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(2);
            }

            //
            [Fact]
            public void ShouldWriteAsDictionary()
            {
                var dict = new Dictionary<object, object>() {{true, "a"}};

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((object) dict);

                mocks.VerifyWrite((byte)(PackStream.TinyMap | dict.Count));
                mocks.VerifyWrite(PackStream.True);
                mocks.VerifyWrite(PackStream.TinyString | 1);
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
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((IList)null);

                mocks.VerifyWrite(PackStream.Null);
            }

            [Theory]
            [InlineData(0x0F, PackStream.TinyList | 0x0F, new byte[0])]
            [InlineData(byte.MaxValue, PackStream.List8, new[] { byte.MaxValue })]
            [InlineData(short.MaxValue, PackStream.List16, new byte[] { 0x7F, 0xFF })]
            [InlineData(int.MaxValue, PackStream.List32, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
            public void ShouldWriteListHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteListHeader(size);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteListOfDifferentTypeCorrectly()
            {
                var list = new List<object>(new object[] {1, true, "a"});

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(list);

                mocks.VerifyWrite((byte)(PackStream.TinyList | list.Count));
                mocks.VerifyWrite(1);
                mocks.VerifyWrite(PackStream.True);
                mocks.VerifyWrite(PackStream.TinyString | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }
        }

        public class WriteDictionaryMethod
        {
            [Fact]
            public void ShouldWriteAsNullIfDictionaryIsNull()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write((IDictionary)null);

                mocks.VerifyWrite(PackStream.Null);
            }

            [Theory]
            [InlineData(0x0F, PackStream.TinyMap | 0x0F, new byte[0])]
            [InlineData(byte.MaxValue, PackStream.Map8, new[] { byte.MaxValue })]
            [InlineData(short.MaxValue, PackStream.Map16, new byte[] { 0x7F, 0xFF })]
            [InlineData(int.MaxValue, PackStream.Map32, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
            public void ShouldWriteListHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteMapHeader(size);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteMapOfDifferentTypeCorrectly()
            {
                var dict = new Dictionary<object, object>() { { true, "a" } };

                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.Write(dict);

                mocks.VerifyWrite((byte)(PackStream.TinyMap | dict.Count));
                mocks.VerifyWrite(PackStream.True);
                mocks.VerifyWrite(PackStream.TinyString | 1);
                mocks.VerifyWrite(new byte[] { 97 });
            }
        }

        public class WriteStructHeaderMethod
        {

            [Theory]
            [InlineData(0x0F, PackStream.TinyStruct | 0x0F, new byte[] { 0x77 })]
            [InlineData(byte.MaxValue, PackStream.Struct8, new byte[] { byte.MaxValue, 0x77 })]
            public void ShouldWriteStructHeaderCorrectly(int size, byte marker, byte[] expected)
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteStructHeader(size, 0x77);

                mocks.VerifyWrite(marker);
                mocks.VerifyWrite(expected);
            }

            [Fact]
            public void ShouldWriteStructHeaderStruct16Correctly()
            {
                var mocks = new WriterTests.Mocks();
                var writer = new PackStreamWriter(mocks.OutputStream);

                writer.WriteStructHeader(short.MaxValue, 0x77);

                mocks.VerifyWrite(PackStream.Struct16);
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
