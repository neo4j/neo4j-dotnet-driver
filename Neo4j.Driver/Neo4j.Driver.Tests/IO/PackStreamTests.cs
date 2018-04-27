// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests.IO.Utils;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class PackStreamTests
    {

        public class Base : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return CreateReaderMachine(bytes, null);
            }

            internal virtual PackStreamReaderMachine CreateReaderMachine(byte[] bytes, IDictionary<byte, IPackStreamStructHandler> structHandlers)
            {
                return new PackStreamReaderMachine(bytes, s => new PackStreamReader(s, structHandlers));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return CreateWriterMachine(null);
            }

            internal virtual PackStreamWriterMachine CreateWriterMachine(IDictionary<Type, IPackStreamStructHandler> structHandlers)
            {
                return new PackStreamWriterMachine(s => new PackStreamWriter(s, structHandlers));
            }

            [Fact]
            public void ShouldReadViaStructHandlerIfThereIsAHandlerRegisteredForSignature()
            {
                var structHandler = new StructTypeStructHandler();
                var structSignature = structHandler.ReadableStructs.First();
                var structHandlerDict =
                    new Dictionary<byte, IPackStreamStructHandler> { { structSignature, structHandler } };

                var writerMachine = CreateWriterMachine();
                writerMachine.Writer().WriteStructHeader(5, structSignature);
                writerMachine.Writer().Write(1L);
                writerMachine.Writer().Write(2L);
                writerMachine.Writer().Write(true);
                writerMachine.Writer().Write(3.0);
                writerMachine.Writer().Write("something");

                var readerMachine = CreateReaderMachine(writerMachine.GetOutput(), structHandlerDict);
                var value = readerMachine.Reader().Read();

                value.Should().NotBeNull();
                value.Should().BeOfType<StructType>();

                var structValue = (StructType)value;
                structValue.Values.Should().NotBeNull();
                structValue.Values.Should().HaveCount(5);
                structValue.Values.Should().Equal(new object[] { 1L, 2L, true, 3.0, "something" });
            }

            [Fact]
            public void ShouldWriteViaStructHandlerIfThereIsAHandlerRegisteredForSignature()
            {
                var structHandler = new StructTypeStructHandler();
                var structType = structHandler.WritableTypes.First();
                var structHandlerDict =
                    new Dictionary<Type, IPackStreamStructHandler> { { structType, structHandler } };

                var writerMachine = CreateWriterMachine(structHandlerDict);
                writerMachine.Writer().Write(new StructType(new List<object> { 1L, 2L, true, 3.0, "something" }));

                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var reader = readerMachine.Reader();

                reader.ReadStructHeader().Should().Be(5);
                reader.ReadStructSignature().Should().Be((byte)'S');
                reader.Read().Should().Be(1L);
                reader.Read().Should().Be(2L);
                reader.Read().Should().Be(true);
                reader.Read().Should().Be(3.0);
                reader.Read().Should().Be("something");
            }

            private class StructType
            {

                public StructType(IList values)
                {
                    Values = values;
                }

                public IList Values { get; }
            }

            private class StructTypeStructHandler : IPackStreamStructHandler
            {
                public IEnumerable<byte> ReadableStructs => new[] { (byte)'S' };

                public IEnumerable<Type> WritableTypes => new[] { typeof(StructType) };

                public object Read(IPackStreamReader reader, byte signature, long size)
                {
                    var values = new List<object>();
                    for (var i = 0; i < size; i++)
                    {
                        values.Add(reader.Read());
                    }

                    return new StructType(values);
                }

                public void Write(IPackStreamWriter writer, object value)
                {
                    var structValue = (StructType)value;

                    writer.WriteStructHeader(structValue.Values.Count, (byte)'S');
                    foreach (var innerValue in structValue.Values)
                    {
                        writer.Write(innerValue);
                    }
                }
            }


        }

        public class V1 : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return new PackStreamReaderMachine(bytes, stream => BoltProtocolPackStream.V1.CreateReader(stream));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return new PackStreamWriterMachine(stream => BoltProtocolPackStream.V1.CreateWriter(stream));
            }
        }

        public class V1NoByteArray : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return new PackStreamReaderMachine(bytes, stream => BoltProtocolPackStream.V1NoByteArray.CreateReader(stream));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return new PackStreamWriterMachine(stream => BoltProtocolPackStream.V1NoByteArray.CreateWriter(stream));
            }

            [Fact(Skip = "Doesn't support byte arrays")]
            public override void ShouldReadWriteByteArray()
            {

            }

            [Fact(Skip = "Doesn't support byte arrays")]
            public override void ShouldReadWriteByteArrayWithVaryingSizes()
            {

            }

            [Fact(Skip = "Doesn't support byte arrays")]
            public override void ShouldWriteNullWhenNullPassedAsByteArray()
            {

            }

            [Fact(Skip = "Doesn't support byte arrays")]
            public override void ShouldReadWriteByteArrayThroughObjectOverload()
            {

            }

            [Fact]
            public void ShouldNotWriteByteArray()
            {
                // Given
                var writerMachine = CreateWriterMachine();

                // When
                var writer = writerMachine.Writer();

                // Then
                var ex = Record.Exception(() =>
                    writer.Write(Encoding.UTF8.GetBytes("ABCDEFGHIJ")));
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ProtocolException>();
            }

            [Fact]
            public void ShouldNotReadByteArray()
            {
                // Given (byte array supporting writer machine)
                var writerMachine = new PackStreamWriterMachine(s => new PackStreamWriter(s, null));

                var writer = writerMachine.Writer();
                writer.Write(Encoding.UTF8.GetBytes("ABCDEFGHIJ"));

                // When
                var readerMachine = CreateReaderMachine(writerMachine.GetOutput());
                var reader = readerMachine.Reader();

                // Then
                var ex = Record.Exception(() => reader.Read());
                ex.Should().NotBeNull();
                ex.Should().BeOfType<ProtocolException>();
            }

        }

        public class V2 : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return new PackStreamReaderMachine(bytes, stream => BoltProtocolPackStream.V2.CreateReader(stream));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return new PackStreamWriterMachine(stream => BoltProtocolPackStream.V2.CreateWriter(stream));
            }
        }

    }
}