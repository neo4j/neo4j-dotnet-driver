// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkReader : IChunkReader
    {
        private class Chunk
		{
            public enum Type
			{
                DataChunk = 0,
                NoopChunk = 1,
                EmptyChunk = 2
			}

            public byte[] ChunkData { get; set; }
            public Type ChunkType { get; set; } 

            public Chunk(byte[] data)
			{
                ChunkData = data;

                if (data != null)
                {
                    if (data.Length > 0)
                        ChunkType = Type.DataChunk;
                    else
                        ChunkType = Type.NoopChunk;
                }
                else
                    ChunkType = Type.EmptyChunk;
			}
        }

        private Stream InputStream { get; set; }
        private ILogger Logger { get; set; }
        
        private const int ChunkHeaderSize = 2;


        public ChunkReader(Stream downStream)
            : this(downStream, null)
        {   
        }

        internal ChunkReader(Stream downStream, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
            Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream));

            InputStream = downStream;
            Logger = logger;
        }
        
        private async Task<byte[]> ReadDataOfSize(int requiredSize)
		{
            var data = new byte[requiredSize];
            int readSize = await InputStream.ReadAsync(data, 0, requiredSize).ConfigureAwait(false);

            if (readSize == 0)
                return null;
            else if (readSize != requiredSize)
                throw new IOException($"Unexpected end of stream, unable to read required data size");
            
            return data;
		}

        private async Task<Chunk> ReadChunk()
		{
            var chunkHeader = new Chunk(await ReadDataOfSize(ChunkHeaderSize));
            
            if (chunkHeader.ChunkType == Chunk.Type.DataChunk)    //if it contains data, then we have a chunk to read
            {
                var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeader.ChunkData);

                if (chunkSize != 0)
                {   
                    var chunk = new Chunk(await ReadDataOfSize(chunkSize));

                    if (chunk.ChunkType == Chunk.Type.EmptyChunk)
                        throw new IOException("Unexpected end of stream, still have an unterminated message");

                    return chunk;
                }

                return new Chunk(new byte[0]);  //Create a no-op chunk
            }
             
            return new Chunk(null); //Create an empty chunk
        }

        private async Task<Chunk.Type> ReadMessage(List<byte[]> messageChunkList)
		{
            Chunk chunk = null;

            while(true)
			{
                chunk = await ReadChunk();

                if (chunk.ChunkType == Chunk.Type.DataChunk)      //There was chunk data to read
                {
                    messageChunkList.Add(chunk.ChunkData);   //Add the data chunk to the message we are building
                }
                else
                    break;             
			}

            return chunk.ChunkType;
		}

        public int ReadNextMessages(Stream outputMessageStream)
        {
            return 0;
        }

        public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
        {
            int messageCount = 0;
            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            while (true)
			{
                var messageData = new List<byte[]>();
                var chunkType = await ReadMessage(messageData);

                if (chunkType != Chunk.Type.EmptyChunk)  //There was data of some kind
                {
                    if (messageData.Count > 0) //There are chunks to construct a message from
                    {
                        messageCount++;   

                        //Loop through the chunk data writing it out into the message stream
                        foreach (var element in messageData)
                        {
                            await outputMessageStream.WriteAsync(element, 0, element.Length);
                        }
                    }
                }
                else //No more data available, so we've got all the messages.
                {
                    break;
                }
            }

            outputMessageStream.Position = previousStreamPosition;
            return messageCount;
        }
    }
}


