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

using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal sealed class MessageReader : IMessageReader
{
    private readonly IChunkReader _chunkReader;
    private readonly int _defaultBufferSize;
    private readonly ILogger _logger;
    private readonly int _maxBufferSize;
    private int _shrinkCounter;
    readonly ByteBuffers _readerBuffers;

    public MemoryStream BufferStream { get; }

    public MessageReader(IChunkReader chunkReader, DriverContext driverContext, ILogger logger)
    {
        _chunkReader = chunkReader;
        _defaultBufferSize = driverContext.Config.DefaultReadBufferSize;
        _maxBufferSize = driverContext.Config.MaxReadBufferSize;
        _logger = logger;
        BufferStream = new MemoryStream(driverContext.Config.MaxReadBufferSize);
        _readerBuffers = new ByteBuffers();
    }

    public async ValueTask ReadAsync(IResponsePipeline pipeline, MessageFormat format)
    {
        var messageCount = await _chunkReader.ReadMessageChunksToBufferStreamAsync(BufferStream).ConfigureAwait(false);
        var psr = new PackStreamReader(format, BufferStream, _readerBuffers);
        ConsumeMessages(pipeline, messageCount, psr);
    }

    public void SetReadTimeoutInMs(int ms)
    {
        _chunkReader.SetTimeoutInMs(ms);
    }

    private void ConsumeMessages(IResponsePipeline pipeline, int messages, PackStreamReader packStreamReader)
    {
        var leftMessages = messages;

        while (packStreamReader.Stream.Length > packStreamReader.Stream.Position && leftMessages > 0)
        {
            ProcessMessage(pipeline, packStreamReader);
            leftMessages -= 1;
        }

        // Check whether we have incomplete message in the buffers
        if (packStreamReader.Stream.Length != packStreamReader.Stream.Position)
        {
            return;
        }

        packStreamReader.Stream.SetLength(0);

        if (packStreamReader.Stream.Capacity <= _maxBufferSize)
        {
            return;
        }

        _logger.Info(
            $@"Shrinking read buffers to the default read buffer size {
                _defaultBufferSize
            } since its size reached {
                packStreamReader.Stream.Capacity
            } which is larger than the maximum read buffer size {
                _maxBufferSize
            }. This has already occurred {_shrinkCounter} times for this connection.");

        _shrinkCounter += 1;

        packStreamReader.Stream.Capacity = _defaultBufferSize;
    }

    private void ProcessMessage(IResponsePipeline pipeline, PackStreamReader packStreamReader)
    {
        var message = packStreamReader.Read();

        if (message is IResponseMessage response)
        {
            response.Dispatch(pipeline);
        }
        else
        {
            throw new ProtocolException($"Unknown response message type {message.GetType().FullName}");
        }
    }
}
