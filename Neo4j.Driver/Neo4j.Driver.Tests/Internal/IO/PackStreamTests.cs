﻿// Copyright (c) "Neo4j"
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
using FluentAssertions;
using Neo4j.Driver.Internal.IO.Utils;
using Neo4j.Driver.Internal.Protocol;
using Xunit;

namespace Neo4j.Driver.Internal.IO
{
    public class PackStreamTests
    {

        public class Base : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return CreateReaderMachine(bytes, null);
            }

            internal virtual PackStreamReaderMachine CreateReaderMachine(byte[] bytes, IDictionary<byte, IPackStreamSerializer> structHandlers)
            {
                return new PackStreamReaderMachine(bytes, s => new PackStreamReader(s, structHandlers));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return CreateWriterMachine(null);
            }

            internal virtual PackStreamWriterMachine CreateWriterMachine(IDictionary<Type, IPackStreamSerializer> structHandlers)
            {
                return new PackStreamWriterMachine(s => new PackStreamWriter(s, structHandlers));
            }

            [Fact]
            public void ShouldReadViaStructHandlerIfThereIsAHandlerRegisteredForSignature()
            {
                var structHandler = new StructTypeSerializer();
                var structSignature = structHandler.ReadableStructs.First();
                var structHandlerDict =
                    new Dictionary<byte, IPackStreamSerializer> { { structSignature, structHandler } };

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
                var structHandler = new StructTypeSerializer();
                var structType = structHandler.WritableTypes.First();
                var structHandlerDict =
                    new Dictionary<Type, IPackStreamSerializer> { { structType, structHandler } };

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

            private class StructTypeSerializer : IPackStreamSerializer
            {
                public IEnumerable<byte> ReadableStructs => new[] { (byte)'S' };

                public IEnumerable<Type> WritableTypes => new[] { typeof(StructType) };

                public object Deserialize(IPackStreamReader reader, byte signature, long size)
                {
                    var values = new List<object>();
                    for (var i = 0; i < size; i++)
                    {
                        values.Add(reader.Read());
                    }

                    return new StructType(values);
                }

                public void Serialize(IPackStreamWriter writer, object value)
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

        public class V3 : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return new PackStreamReaderMachine(bytes, stream => BoltProtocolMessageFormat.V3.CreateReader(stream));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return new PackStreamWriterMachine(stream => BoltProtocolMessageFormat.V3.CreateWriter(stream));
            }
        }

        public class V4 : PackStreamTestSpecs
        {
            internal override PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
            {
                return new PackStreamReaderMachine(bytes, stream => BoltProtocolMessageFormat.V4.CreateReader(stream));
            }

            internal override PackStreamWriterMachine CreateWriterMachine()
            {
                return new PackStreamWriterMachine(stream => BoltProtocolMessageFormat.V4.CreateWriter(stream));
            }
        }

    }
}