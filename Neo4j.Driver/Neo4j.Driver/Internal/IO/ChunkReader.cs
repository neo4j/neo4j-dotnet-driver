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
using System.Threading;
using System.Threading.Tasks;


namespace Neo4j.Driver.Internal.IO
{
    internal class ChunkReader : IChunkReader
    {
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
            byte[] data = new byte[requiredSize];
            int readSize = await InputStream.ReadAsync(data, 0, requiredSize).ConfigureAwait(false);

            if (readSize == 0)
                return null;
            if(readSize != requiredSize)
                throw new IOException($"Unexpected end of stream, unable to read required data size");
            
            return data;
		}

        private async Task<byte[]> ReadChunk()
		{
            var chunkHeaderData = await ReadDataOfSize(ChunkHeaderSize);
            
            if (chunkHeaderData != null)    //if it is null there is no more data to read
            {
                var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeaderData);

                if (chunkSize != 0)
                {   
                    var chunkData = await ReadDataOfSize(chunkSize);

                    if (chunkData == null)
                        throw new IOException("Unexpected end of stream, still have an unterminated message");

                    return chunkData;
                }

                //return new byte[0];  //TODO Not sure if I should return an empty array or null when it is a zero chunk size (NOOP).
                //or maybe some kind of open message -> close message system like before.
            }
             
            return null;
        }

        private async Task<List<byte[]>> ReadMessage()
		{
            List<byte[]> messageChunkData = new List<byte[]>();

            while(true)
			{
                var chunk = await ReadChunk();

                if (chunk != null)      //There was chunk data to read
                {
                    messageChunkData.Add(chunk);   //Add the data chunk to the message we are building
                }
                else
                    break;  //Zero chunk size                
			}

            return messageChunkData;            
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
                var messageData = await ReadMessage();

                if (messageData.Count > 0)  //There are chunks to construct a message from
                {
                    messageCount++;   //There were chunks read so we have a message, no count means a NOOP.

                    //Loop through the chunk data writing it out into the message stream
                    foreach (var element in messageData)
                    {
                        await outputMessageStream.WriteAsync(element, 0, element.Length);
                    }
                }
                else
                    break;  //No more messages                
            }

            outputMessageStream.Position = previousStreamPosition;
            return messageCount;
        }
    }
}


