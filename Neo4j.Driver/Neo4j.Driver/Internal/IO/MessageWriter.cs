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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver;
using Neo4j.Driver.Internal.Protocol;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class MessageWriter: IMessageWriter
    {
        private readonly IChunkWriter _chunkWriter;
        private readonly IPackStreamWriter _packStreamWriter;
        
        public MessageWriter(Stream stream, IMessageFormat messageFormat)
            : this(stream, Constants.DefaultWriteBufferSize, Constants.MaxWriteBufferSize, messageFormat)
        {
            
        }

        public MessageWriter(Stream stream, int defaultBufferSize, int maxBufferSize, IMessageFormat messageFormat)
            : this(stream, defaultBufferSize, maxBufferSize, null, messageFormat)
        {

        }
        
        public MessageWriter(Stream stream, BufferSettings bufferSettings, ILogger logger, BoltProtocolVersion version, IDictionary<string, string> routingContext)
        {
            _chunkWriter = new ChunkWriter(stream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger);
            _packStreamWriter = BoltProtocolFactory
                .ForVersion(version, routingContext)
                .MessageFormat
                .CreateWriter(_chunkWriter.ChunkerStream);
        }

        public MessageWriter(Stream stream, int defaultBufferSize, int maxBufferSize, ILogger logger, IMessageFormat messageFormat)
        {
            Throw.ArgumentNullException.IfNull(stream, nameof(stream));
            Throw.ArgumentOutOfRangeException.IfFalse(stream.CanWrite, nameof(stream));
            Throw.ArgumentNullException.IfNull(messageFormat, nameof(messageFormat));

            _chunkWriter = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger);
            _packStreamWriter = messageFormat.CreateWriter(_chunkWriter.ChunkerStream);
        }

        public void Write(IRequestMessage message)
        {
            _chunkWriter.OpenChunk();
            _packStreamWriter.Write(message);
            _chunkWriter.CloseChunk();

            // add message boundary
            _chunkWriter.OpenChunk();
            _chunkWriter.CloseChunk();
        }

        public Task FlushAsync()
        {
            return _chunkWriter.SendAsync();
        }
        
    }
}
