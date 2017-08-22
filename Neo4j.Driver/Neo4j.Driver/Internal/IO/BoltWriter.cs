// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class BoltWriter: IBoltWriter, IMessageRequestHandler
    {
        private static readonly Dictionary<string, object> EmptyDictionary = new Dictionary<string, object>();
        private static readonly byte[] MessageBoundary = new byte[0];

        private readonly IChunkWriter _chunkWriter;
        private readonly PackStreamWriter _packStreamWriter;
        private readonly ILogger _logger;

        public BoltWriter(Stream stream)
            : this(stream, true)
        {
            
        }

        public BoltWriter(Stream stream, bool supportBytes)
            : this(stream, Constants.DefaultWriteBufferSize, Constants.MaxWriteBufferSize, supportBytes)
        {
            
        }

        public BoltWriter(Stream stream, int defaultBufferSize, int maxBufferSize, bool supportBytes)
            : this(stream, defaultBufferSize, maxBufferSize, null, supportBytes)
        {

        }

        public BoltWriter(Stream stream, int defaultBufferSize, int maxBufferSize, ILogger logger, bool supportBytes)
        {
            Throw.ArgumentNullException.IfNull(stream, nameof(stream));

            _logger = logger;
            _chunkWriter = new ChunkWriter(stream, defaultBufferSize, maxBufferSize, logger);
            _packStreamWriter = supportBytes ? new PackStreamWriter(_chunkWriter.ChunkerStream) : new PackStreamWriterBytesIncompatible(_chunkWriter.ChunkerStream);
        }

        public void Write(IRequestMessage message)
        {
            _chunkWriter.OpenChunk();

            message.Dispatch(this);

            _chunkWriter.CloseChunk();

            // add message boundary
            _chunkWriter.OpenChunk();
            _chunkWriter.CloseChunk();
        }

        public void Flush()
        {
            _chunkWriter.Send();
        }

        public Task FlushAsync()
        {
            return _chunkWriter.SendAsync();
        }
        
        public void HandleInitMessage(string clientNameAndVersion, IDictionary<string, object> authToken)
        {
            _packStreamWriter.WriteStructHeader(1, MsgInit);
            _packStreamWriter.Write(clientNameAndVersion);
            _packStreamWriter.Write(authToken ?? EmptyDictionary);
        }

        public void HandleRunMessage(string statement, IDictionary<string, object> parameters)
        {
            _packStreamWriter.WriteStructHeader(2, MsgRun);
            _packStreamWriter.Write(statement);
            _packStreamWriter.Write(parameters ?? EmptyDictionary);
        }

        public void HandlePullAllMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgPullAll);
        }

        public void HandleDiscardAllMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgDiscardAll);
        }

        public void HandleResetMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgReset);
        }

        public void HandleAckFailureMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgAckFailure);
        }
        
    }
}
