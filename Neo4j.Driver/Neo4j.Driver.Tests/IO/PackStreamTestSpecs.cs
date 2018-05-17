// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.IO.Utils;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Tests.TestUtil.CollectionExtensionsTests;

namespace Neo4j.Driver.Tests.IO
{
    public abstract class PackStreamTestSpecs
    {
        internal abstract PackStreamWriterMachine CreateWriterMachine();
        internal abstract PackStreamReaderMachine CreateReaderMachine(byte[] bytes);

        [Fact]
        public void ShouldReadWriteNull()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().WriteNull();

            // Then
            var bytes = writerMachine.GetOutput();
            Assert.Equal(bytes, new byte[] {0xC0});

            // When
            var readerMachine = CreateReaderMachine(bytes);
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().Read();

            // Then
            packedType.Should().Be(PackStream.PackType.Null);
            packedValue.Should().BeNull();
        }

        [Fact]
        public void ShouldReadWriteBooleanTrue()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().Write(true);

            // Then
            var bytes = writerMachine.GetOutput();
            Assert.Equal(bytes, new byte[] {0xC3});

            // When
            var readerMachine = CreateReaderMachine(bytes);
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadBoolean();

            // Then
            packedType.Should().Be(PackStream.PackType.Boolean);
            packedValue.Should().BeTrue();
        }

        [Fact]
        public void ShouldReadWriteBooleanFalse()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().Write(false);

            // Then
            var bytes = writerMachine.GetOutput();
            Assert.Equal(bytes, new byte[] {0xC2});

            // When
            var readerMachine = CreateReaderMachine(bytes);
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadBoolean();

            // Then
            packedType.Should().Be(PackStream.PackType.Boolean);
            packedValue.Should().BeFalse();
        }

        [Fact]
        public void ShouldReadWriteIntegerTiny()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (long i = -16; i < 128; i++)
            {
                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(i);

                // Then
                var bytes = writerMachine.GetOutput();
                Assert.Single(bytes);

                // When
                var readerMachine = CreateReaderMachine(bytes);
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadLong();

                // Then
                packedType.Should().Be(PackStream.PackType.Integer);
                packedValue.Should().Be(i);
            }
        }

        [Fact]
        public void ShouldReadWriteIntegerShort()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (long i = -32768; i < 32768; i++)
            {
                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(i);

                // Then
                var bytes = writerMachine.GetOutput();
                Assert.InRange(bytes.Length, 1, 3);

                // When
                var readerMachine = CreateReaderMachine(bytes);
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadLong();

                // Then
                packedType.Should().Be(PackStream.PackType.Integer);
                packedValue.Should().Be(i);
            }
        }

        [Fact]
        public void ShouldReadWriteIntegerPowersOfTwo()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (var i = 0; i < 32; i++)
            {
                var n = (long) Math.Pow(2, i);

                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(n);

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadLong();

                // Then
                packedType.Should().Be(PackStream.PackType.Integer);
                packedValue.Should().Be(n);
            }
        }

        [Fact]
        public void ShouldReadWriteIntegerAsInteger()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (var i = 0; i < 31; i++)
            {
                var n = (long) Math.Pow(2, i);

                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(n);

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadInteger();

                // Then
                packedType.Should().Be(PackStream.PackType.Integer);
                packedValue.Should().Be((int) n);
            }
        }

        [Fact]
        public void ShouldNotReadLargeLongAsInteger()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            var n = (long) Math.Pow(2, 32);

            // When
            writerMachine.Reset();
            writerMachine.Writer().Write(n);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var ex = Record.Exception(() => readerMachine.Reader().ReadInteger());

            ex.Should().NotBeNull();
            ex.Should().BeOfType<OverflowException>();
        }

        [Fact]
        public void ShouldReadWriteDoublePowersOfTwoPlusABit()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (var i = 0; i < 32; i++)
            {
                var n = Math.Pow(2, i) + 0.5;

                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(n);

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadDouble();

                // Then
                packedType.Should().Be(PackStream.PackType.Float);
                packedValue.Should().Be(n);
            }
        }

        [Fact]
        public void ShouldReadWriteDoublePowersOfTwoMinusABit()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (var i = 0; i < 32; i++)
            {
                var n = Math.Pow(2, i) - 0.5;

                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(n);

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadDouble();

                // Then
                packedType.Should().Be(PackStream.PackType.Float);
                packedValue.Should().Be(n);
            }
        }

        [Fact]
        public void ShouldReadWriteStringsWithVaryingSizes()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            for (var i = 0; i < 24; i++)
            {
                var str = new string(' ', (int) Math.Pow(2, i));

                // When
                writerMachine.Reset();
                writerMachine.Writer().Write(str);

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var packedType = readerMachine.Reader().PeekNextType();
                var packedValue = readerMachine.Reader().ReadString();

                // Then
                packedType.Should().Be(PackStream.PackType.String);
                packedValue.Should().Be(str);
            }
        }

        [Fact]
        public virtual void ShouldReadWriteByteArray()
        {
            var bytes = Encoding.UTF8.GetBytes("ABCDEFGHIJ");

            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(bytes);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadBytes();

            // Then
            packedType.Should().Be(PackStream.PackType.Bytes);
            packedValue.Should().Equal(bytes);
        }

        [Fact]
        public virtual void ShouldReadWriteByteArrayWithVaryingSizes()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            VerifyReadWriteByteArray(writerMachine, 0);
            for (var i = 0; i < 24; i++)
            {
                VerifyReadWriteByteArray(writerMachine, (int) Math.Pow(2, i));
            }
        }

        [Fact]
        public void ShouldReadWriteChar()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().Write('A');

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadString();

            // Then
            packedType.Should().Be(PackStream.PackType.String);
            packedValue.Should().Be("A");
        }

        [Fact]
        public void ShouldReadWriteString()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().Write("ABCDEFGHIJ");

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadString();

            // Then
            packedType.Should().Be(PackStream.PackType.String);
            packedValue.Should().Be("ABCDEFGHIJ");
        }

        [Fact]
        public void ShouldReadWriteStringWithSpecialCharacters()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            writerMachine.Writer().Write("Mjölnir");

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadString();

            // Then
            packedType.Should().Be(PackStream.PackType.String);
            packedValue.Should().Be("Mjölnir");
        }

        [Fact]
        public void ShouldReadWriteListItemByItem()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteListHeader(3);
            writer.Write(12);
            writer.Write(13);
            writer.Write(14);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(3);
            reader.ReadLong().Should().Be(12);
            reader.ReadLong().Should().Be(13);
            reader.ReadLong().Should().Be(14);
        }

        [Fact]
        public void ShouldReadWriteListOfString()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(new List<string>(new[] {"one", "two", "three"}));

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(3);
            reader.ReadString().Should().Be("one");
            reader.ReadString().Should().Be("two");
            reader.ReadString().Should().Be("three");
        }

        [Fact]
        public void ShouldReadWriteListOfStringItemByItem()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteListHeader(3);
            writer.Write("one");
            writer.Write("two");
            writer.Write("three");

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(3);
            reader.ReadString().Should().Be("one");
            reader.ReadString().Should().Be("two");
            reader.ReadString().Should().Be("three");
        }

        [Fact]
        public void ShouldReadWriteListOfValues()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(new object[] {1, 2.0, "three", false, 'A'}.ToList());

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(5);
            reader.ReadLong().Should().Be(1);
            reader.ReadDouble().Should().Be(2.0);
            reader.ReadString().Should().Be("three");
            reader.ReadBoolean().Should().BeFalse();
            reader.ReadString().Should().Be("A");
        }

        [Fact]
        public void ShouldReadWriteListOfValuesItemByItem()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteListHeader(4);
            writer.Write(1);
            writer.Write(2.0);
            writer.Write("three");
            writer.Write(false);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            Assert.Equal(PackStream.PackType.List, reader.PeekNextType());
            reader.ReadListHeader().Should().Be(4);
            reader.ReadLong().Should().Be(1);
            reader.ReadDouble().Should().Be(2.0);
            reader.ReadString().Should().Be("three");
            reader.ReadBoolean().Should().BeFalse();
        }

        [Fact]
        public void ShouldReadWriteListOfStringWithSpecialCharactersItemByItem()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteListHeader(3);
            writer.Write("Mjölnir");
            writer.Write("Häagen-Dazs");
            writer.Write("Muğla");

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(3);
            reader.ReadString().Should().Be("Mjölnir");
            reader.ReadString().Should().Be("Häagen-Dazs");
            reader.ReadString().Should().Be("Muğla");
        }

        [Fact]
        public void ShouldReadWriteListOfStringWithSpecialCharactersInBatches()
        {
            VerifyReadWriteListOfString(3, "üç");
            VerifyReadWriteListOfString(126, "yirmidört");
            VerifyReadWriteListOfString(3000, "mjölnir");
            VerifyReadWriteListOfString(32768, "kırk");
        }

        [Fact]
        public void ShouldReadWriteMap()
        {
            VerifyReadWriteMap(2);
            VerifyReadWriteMap(126);
            VerifyReadWriteMap(2439);
            VerifyReadWriteMap(32768);
        }

        [Fact]
        public void ShouldReadWriteStruct()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteStructHeader(3, (byte) 'N');
            writer.Write(12);
            writer.Write(new[] {"Person", "Employee"}.ToList());
            writer.Write(new Dictionary<string, object>()
            {
                {"name", "Alice"},
                {"age", 33}
            });

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(3);
            reader.ReadStructSignature().Should().Be((byte) 'N');

            reader.ReadLong().Should().Be(12);

            reader.ReadListHeader().Should().Be(2);
            reader.ReadString().Should().Be("Person");
            reader.ReadString().Should().Be("Employee");

            reader.ReadMapHeader().Should().Be(2);
            reader.ReadString().Should().Be("name");
            reader.ReadString().Should().Be("Alice");
            reader.ReadString().Should().Be("age");
            reader.ReadLong().Should().Be(33);
        }

        [Fact]
        public void ShouldReadWriteStructWithVaryingSizes()
        {
            VerifyReadWriteStruct(2);
            VerifyReadWriteStruct(126);
            VerifyReadWriteStruct(2439);

            var ex = Record.Exception(() => VerifyReadWriteStruct(65536));
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldReadWriteListWithLists()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(new object[] {1, 2, 3, new[] {4, 5}.ToList()}.ToList());

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(4);
            reader.ReadLong().Should().Be(1);
            reader.ReadLong().Should().Be(2);
            reader.ReadLong().Should().Be(3);
            reader.ReadListHeader().Should().Be(2);
            reader.ReadLong().Should().Be(4);
            reader.ReadLong().Should().Be(5);
        }

        [Fact]
        public void ShouldReadWriteStructWithLists()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteStructHeader(4, (byte) '~');
            writer.Write(1);
            writer.Write(2);
            writer.Write(3);
            writer.Write(new[] {4, 5}.ToList());

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(4);
            reader.ReadStructSignature().Should().Be((byte) '~');

            reader.ReadLong().Should().Be(1);
            reader.ReadLong().Should().Be(2);
            reader.ReadLong().Should().Be(3);
            reader.ReadListHeader().Should().Be(2);
            reader.ReadLong().Should().Be(4);
            reader.ReadLong().Should().Be(5);
        }

        [Fact]
        public void ShouldReadWriteMapWithLists()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteMapHeader(2);
            writer.Write("name");
            writer.Write("Bob");
            writer.Write("catages");
            writer.Write(new object[] {4.3, true}.ToList());

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.Map);
            reader.ReadMapHeader().Should().Be(2);
            reader.ReadString().Should().Be("name");
            reader.ReadString().Should().Be("Bob");
            reader.ReadString().Should().Be("catages");
            reader.ReadListHeader().Should().Be(2);
            reader.ReadDouble().Should().Be(4.3);
            reader.ReadBoolean().Should().BeTrue();
        }

        [Fact]
        public void ShouldPeekNextType()
        {
            VerifyPeekType(PackStream.PackType.Null, null);
            VerifyPeekType(PackStream.PackType.String, "a string");
            VerifyPeekType(PackStream.PackType.Boolean, true);
            VerifyPeekType(PackStream.PackType.Float, 123.123);
            VerifyPeekType(PackStream.PackType.Integer, 123);
            VerifyPeekType(PackStream.PackType.List, new[] {1, 2, 3}.ToList());
            VerifyPeekType(PackStream.PackType.Map, new Dictionary<string, object> {{"key", 1}});
        }

        [Fact]
        public void ShouldWriteNullWhenNullPassed()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write((string) null);
            writer.Write((IDictionary) null);
            writer.Write((IList) null);
            writer.Write((IDictionary<string, int>) null);
            writer.Write((IList<string>) null);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.Read().Should().BeNull();
            reader.Read().Should().BeNull();
            reader.Read().Should().BeNull();
            reader.Read().Should().BeNull();
            reader.Read().Should().BeNull();
        }

        [Fact]
        public virtual void ShouldWriteNullWhenNullPassedAsByteArray()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write((byte[]) null);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.Read().Should().BeNull();
        }

        [Fact]
        public void ShouldNotWriteUnknownType()
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            var ex = Record.Exception(() => writer.Write(new UnsupportedType()));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

        [Fact]
        public void ShouldReadWriteThroughObjectOverload()
        {
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Boolean, true);
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Boolean, false);
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.String, "a string");
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Float, 123.123);
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Integer, 123L);
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.List, new object[] {1L, 2L, 3L}.ToList());
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Map, new Dictionary<string, object> {{"key", 1L}});
        }

        [Fact]
        public void ShouldWriteGenericListCorrectly()
        {
            var list = new List<int>{1, 2, 3};

            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();

            writer.Write((object)list);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.ReadStructSignature().Should().Be((byte)(PackStream.TinyList | list.Count));
            reader.ReadLong().Should().Be(1);
            reader.ReadLong().Should().Be(2);
            reader.ReadLong().Should().Be(3);
        }

        [Fact]
        public void ShouldWriteListOfEnumerableTypeCorrectly()
        {
            var nums = new MyCollection<int>(new[] {1, 2, 3});

            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(nums);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            reader.ReadStructSignature().Should().Be((byte)(PackStream.TinyList | nums.Count()));
            reader.ReadLong().Should().Be(1);
            reader.ReadLong().Should().Be(2);
            reader.ReadLong().Should().Be(3);
        }

        [Fact]
        public void ShouldWriteListOfEnumerableOfRandomTypeCorrectly()
        {
            var values = new MyCollection<object>(new object[]
            {
                1,
                new[] {2, 3},
                'a'
            });

            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(values);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            reader.ReadStructSignature().Should().Be((byte) (PackStream.TinyList | values.Count()));
            reader.ReadLong().Should().Be(1);
            reader.ReadStructSignature().Should().Be((byte) (PackStream.TinyList | 2));
            reader.ReadLong().Should().Be(2);
            reader.ReadLong().Should().Be(3);
            reader.ReadStructSignature().Should().Be(PackStream.TinyString | 1);
            reader.ReadLong().Should().Be(97);
        }

        [Fact]
        public virtual void ShouldReadWriteByteArrayThroughObjectOverload()
        {
            VerifyReadWriteThroughObjectOverload(PackStream.PackType.Bytes, new byte[] {1, 2, 3});
        }

        private void VerifyReadWriteByteArray(PackStreamWriterMachine machine, int length)
        {
            var array = new byte[length];

            machine.Reset();
            machine.Writer().Write(array);

            var readerMachine = CreateReaderMachine(machine.GetOutput());
            var packedType = readerMachine.Reader().PeekNextType();
            var packedValue = readerMachine.Reader().ReadBytes();

            // Then
            packedType.Should().Be(PackStream.PackType.Bytes);
            packedValue.Should().Equal(array);
        }

        private void VerifyReadWriteListOfString(int size, string value)
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(Enumerable.Range(0, size).Select(i => value).ToList());

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.List);
            reader.ReadListHeader().Should().Be(size);
            for (int i = 0; i < size; i++)
            {
                reader.ReadString().Should().Be(value);
            }
        }

        private void VerifyReadWriteMap(int size)
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < size; i++)
            {
                dict[i.ToString()] = i;
            }

            writer.Write(dict);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.Map);
            reader.ReadMapHeader().Should().Be(size);
            for (var i = 0; i < size; i++)
            {
                reader.ReadString().Should().Be(i.ToString());
                reader.ReadLong().Should().Be(i);
            }
        }

        private void VerifyReadWriteStruct(int size)
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.WriteStructHeader(size, (byte) 'N');
            for (var i = 0; i < size; i++)
            {
                writer.Write(i);
            }

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(PackStream.PackType.Struct);
            reader.ReadStructHeader().Should().Be(size);
            reader.ReadStructSignature().Should().Be((byte) 'N');
            for (var i = 0; i < size; i++)
            {
                reader.ReadLong().Should().Be(i);
            }
        }

        private void VerifyPeekType(PackStream.PackType type, object value)
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write(value);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();

            // Then
            reader.PeekNextType().Should().Be(type);
        }

        private void VerifyReadWriteThroughObjectOverload(PackStream.PackType type, object value)
        {
            // Given
            var writerMachine = CreateWriterMachine();

            // When
            var writer = writerMachine.Writer();
            writer.Write((object) value);

            // When
            var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
            var reader = readerMachine.Reader();
            var packedType = reader.PeekNextType();
            var packedValue = reader.Read();

            // Then
            packedType.Should().Be(type);
            switch (value)
            {
                case IDictionary dict:
                    Assert.Equal(dict, packedValue);
                    break;
                case IList list:
                    Assert.Equal(list, packedValue);
                    break;
                default:
                    value.Should().Be(packedValue);
                    break;
            }
        }

        private class UnsupportedType
        {
        }

    }
}
