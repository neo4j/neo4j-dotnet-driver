// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Linq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO.Utils;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests;

namespace Neo4j.Driver.Internal.IO;

public abstract class PackStreamSerializerTests
{
    internal abstract IPackStreamSerializer SerializerUnderTest { get; }

    internal virtual IEnumerable<IPackStreamSerializer> SerializersNeeded =>
        Enumerable.Empty<IPackStreamSerializer>();

    internal virtual PackStreamWriterMachine CreateWriterMachine()
    {
        var writerHandlersDict = SerializersNeeded.Union(new[] { SerializerUnderTest })
            .SelectMany(
                h => h.WritableTypes,
                (handler, type) => new KeyValuePair<Type, IPackStreamSerializer>(type, handler))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var format = new MessageFormat(writerHandlersDict);

        return new PackStreamWriterMachine(stream => new PackStreamWriter(format, stream));
    }

    internal virtual PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
    {
        var readerHandlersDict = SerializersNeeded.Union(new[] { SerializerUnderTest })
            .SelectMany(
                h => h.ReadableStructs,
                (handler, signature) => new KeyValuePair<byte, IPackStreamSerializer>(signature, handler))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var format = new MessageFormat(null, readerHandlersDict);

        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(format, stream, new ByteBuffers()));
    }

    internal SpanPackStreamReader CreateSpanReader(byte[] bytes)
    {
        var data = new Span<byte>(bytes);
        var messageFormat = new MessageFormat(new[] { SerializerUnderTest }.Concat(SerializersNeeded));
        return new SpanPackStreamReader(messageFormat, data);
    }

    internal PackStreamWriterMachine CreateWriterMachine(BoltProtocolVersion version)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);

        return new PackStreamWriterMachine(stream => new PackStreamWriter(format, stream));
    }

    internal PackStreamReaderMachine CreateReaderMachine(BoltProtocolVersion version, byte[] bytes)
    {
        var format = new MessageFormat(version, TestDriverContext.MockContext);
        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(format, stream, new ByteBuffers()));
    }
}
