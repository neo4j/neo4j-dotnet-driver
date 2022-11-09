// Copyright (c) 2002-2022 "Neo4j,"
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

using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal class MessageWriter : IMessageWriter
{
    public MessageWriter(ChunkWriter chunkWriter, IMessageFormat format)
    {
        _chunkWriter = chunkWriter;
        _packStreamWriter = new PackStreamWriter(format, chunkWriter);
    }

    private readonly IChunkWriter _chunkWriter;
    private readonly PackStreamWriter _packStreamWriter;

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