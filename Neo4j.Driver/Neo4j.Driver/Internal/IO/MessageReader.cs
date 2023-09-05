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

using System.Threading.Tasks;
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

    public MessageReader(IChunkReader chunkReader, BufferSettings bufferSettings, ILogger logger)
    {
        _chunkReader = chunkReader;
        _defaultBufferSize = bufferSettings.DefaultReadBufferSize;
        _maxBufferSize = bufferSettings.MaxReadBufferSize;
        _logger = logger;
    }

    public async ValueTask ReadAsync(IResponsePipeline pipeline, PackStreamReader reader)
    {
        var messageCount = await _chunkReader.ReadMessageChunksToBufferStreamAsync(reader.Stream).ConfigureAwait(false);
        ConsumeMessages(pipeline, messageCount, reader);
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

        _logger?.Info(
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
