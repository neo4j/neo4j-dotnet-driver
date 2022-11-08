// Copyright (c) "Neo4j"
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

using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO
{
    internal class MessageReader : IMessageReader
    {
        private readonly IChunkReader _chunkReader;
        private readonly PackStreamReader _packStreamReader;
        private readonly ILogger _logger;
        private readonly MemoryStream _bufferStream;
        private readonly int _defaultBufferSize;
        private readonly int _maxBufferSize;
        private int _shrinkCounter = 0;

        public MessageReader(IConnection owner, MessageFormat format, IChunkReader chunkReader, BufferSettings bufferSettings, ILogger logger)
        {
            _bufferStream = new MemoryStream(bufferSettings.DefaultReadBufferSize);
            _defaultBufferSize = bufferSettings.DefaultReadBufferSize;
            _maxBufferSize = bufferSettings.MaxReadBufferSize;
            _packStreamReader = new PackStreamReader(owner, _bufferStream, format);
            _chunkReader = chunkReader;
            _logger = logger;
        }

        public async Task ReadAsync(IResponsePipeline pipeline)
        {
            var messageCount = await _chunkReader.ReadNextMessagesAsync(_bufferStream).ConfigureAwait(false);
            ConsumeMessages(pipeline, messageCount);
        }

        private void ConsumeMessages(IResponsePipeline pipeline, int messages)
        {
            var leftMessages = messages;

            while (_bufferStream.Length > _bufferStream.Position && leftMessages > 0)
            {
                ProcessMessage(pipeline);

                leftMessages -= 1;
            }

            // Check whether we have incomplete message in the buffers
            if (_bufferStream.Length != _bufferStream.Position)
                return;
            
            _bufferStream.SetLength(0);

            if (_bufferStream.Capacity <= _maxBufferSize)
                return;
            
            _logger?.Info(
                $@"Shrinking read buffers to the default read buffer size {
                    _defaultBufferSize
                } since its size reached {
                    _bufferStream.Capacity
                } which is larger than the maximum read buffer size {
                    _maxBufferSize
                }. This has already occurred {_shrinkCounter} times for this connection.");

            _shrinkCounter += 1;

            _bufferStream.Capacity = _defaultBufferSize;
        }

        private void ProcessMessage(IResponsePipeline pipeline)
        {
            var message = _packStreamReader.Read();

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
}