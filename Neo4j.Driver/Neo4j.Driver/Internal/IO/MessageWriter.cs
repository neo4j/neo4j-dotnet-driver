﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal sealed class MessageWriter : IMessageWriter
{
    private readonly ChunkWriter _chunkWriter;

    public MessageWriter(ChunkWriter chunkWriter)
    {
        _chunkWriter = chunkWriter;
    }

    public void Write(IRequestMessage message, PackStreamWriter writer)
    {
        _chunkWriter.OpenChunk();
        writer.Write(message);
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
