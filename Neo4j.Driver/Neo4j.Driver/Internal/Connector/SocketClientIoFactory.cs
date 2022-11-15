// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using System.IO;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Connector;

internal class SocketClientIoFactory : IConnectionIoFactory
{
    public ITcpSocketClient TcpSocketClient(SocketSettings socketSettings, ILogger logger)
    {
        return new TcpSocketClient(socketSettings, logger);
    }

    public (MessageFormat Format, ChunkWriter ChunkWriter, MemoryStream readBuffer, IMessageReader
        MessageReader, IMessageWriter MessageWriter) Build(
            ITcpSocketClient socketClient,
            BufferSettings bufferSettings,
            ILogger logger,
            BoltProtocolVersion version)
    {
        var format = new MessageFormat(version);
        var chunkReader = new ChunkReader(socketClient.ReaderStream);
        var chunkWriter = new ChunkWriter(socketClient.WriterStream, bufferSettings, logger);
        var readBuffer = new MemoryStream(bufferSettings.MaxReadBufferSize);
        var messageReader = new MessageReader(chunkReader, bufferSettings, logger);
        var messageWriter = new MessageWriter(chunkWriter);

        return (format, chunkWriter, readBuffer, messageReader, messageWriter);
    }
}
