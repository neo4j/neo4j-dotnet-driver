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

using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Connector;

internal interface IConnectionIoFactory
{
    ITcpSocketClient TcpSocketClient(DriverContext context, INeo4jLogger neo4JLogger);
    MessageFormat Format(BoltProtocolVersion version, DriverContext context);

    IMessageReader MessageReader(
        ITcpSocketClient client,
        DriverContext context,
        INeo4jLogger neo4JLogger);

    (IChunkWriter, IMessageWriter) Writers(ITcpSocketClient client, DriverContext context, INeo4jLogger neo4JLogger);
}

internal sealed class SocketClientIoFactory : IConnectionIoFactory
{
    internal static readonly SocketClientIoFactory Default = new();

    private SocketClientIoFactory()
    {
    }

    public ITcpSocketClient TcpSocketClient(DriverContext context, INeo4jLogger neo4JLogger)
    {
        return new TcpSocketClient(context, neo4JLogger);
    }

    public MessageFormat Format(BoltProtocolVersion version, DriverContext context)
    {
        return new MessageFormat(version, context);
    }

    public IMessageReader MessageReader(
        ITcpSocketClient client,
        DriverContext context,
        INeo4jLogger neo4JLogger)
    {
        if (context.Config.MessageReaderConfig.DisablePipelinedMessageReader)
        {
            return new MessageReader(new ChunkReader(client.ReaderStream), context, neo4JLogger);
        }

        return new PipelinedMessageReader(client.ReaderStream, context);
    }

    public (IChunkWriter, IMessageWriter) Writers(ITcpSocketClient client, DriverContext context, INeo4jLogger neo4JLogger)
    {
        var chunkWriter = new ChunkWriter(client.WriterStream, context, neo4JLogger);
        var messageWriter = new MessageWriter(chunkWriter);
        return (chunkWriter, messageWriter);
    }
}
