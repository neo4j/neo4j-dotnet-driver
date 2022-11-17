// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using System.Linq;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO.Utils;

namespace Neo4j.Driver.Internal.IO;

public abstract class PackStreamSerializerTests
{
    internal abstract IPackStreamSerializer SerializerUnderTest { get; }

    internal virtual IEnumerable<IPackStreamSerializer> SerializersNeeded =>
        Enumerable.Empty<IPackStreamSerializer>();

    internal virtual PackStreamWriterMachine CreateWriterMachine()
    {
        var format = new MessageFormat(SerializerUnderTest, SerializersNeeded);

        var settings = new BufferSettings(Config.Default);
        var logger = new Mock<ILogger>().Object;

        return new PackStreamWriterMachine(
            stream => new PackStreamWriter(format, new ChunkWriter(stream, settings, logger)));
    }

    internal virtual PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
    {
        var format = new MessageFormat(SerializerUnderTest, SerializersNeeded);

        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(stream, format, new ByteBuffers()));
    }

    internal PackStreamWriterMachine CreateWriterMachine(BoltProtocolVersion version)
    {
        var format = new MessageFormat(version);

        var settings = new BufferSettings(Config.Default);
        var logger = new Mock<ILogger>().Object;

        return new PackStreamWriterMachine(
            stream => new PackStreamWriter(format, new ChunkWriter(stream, settings, logger)));
    }

    internal PackStreamReaderMachine CreateReaderMachine(BoltProtocolVersion version, byte[] bytes)
    {
        var format = new MessageFormat(version);
        return new PackStreamReaderMachine(
            bytes,
            stream => new PackStreamReader(stream, format, new ByteBuffers()));
    }
}
