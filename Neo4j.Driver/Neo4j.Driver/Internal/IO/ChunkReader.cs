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

        enum ChunkType
        {     
            ZeroChunk = 0,
            NonZeroChunk = 1,
            NumChunkTypes = 2
        }


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
            var chunkData = await ReadDataOfSize(ChunkHeaderSize);
            if(chunkData == null)
                throw new IOException($"Unexpected end of stream, unable to read required data size");

            var chunkSize = PackStreamBitConverter.ToUInt16(chunkData);

            if (chunkSize != 0)
            {
                chunkData = await ReadDataOfSize(chunkSize);

                if (chunkData == null)
                    throw new IOException("Unexpected end of stream, still have an unterminated message");

                return chunkData;
            }
             
            return null;
        }

        private async Task<List<byte[]>> ReadMessage()
		{
            var messageData = new List<byte[]>();

            while(true)
			{
                var chunk = await ReadChunk();

                if (chunk != null)
                    messageData.Add(chunk);
                else
                    break;
			}

            return messageData;            
		}

        public int ReadNextMessages(Stream outputMessageStream)
        {
            return 0;
        }

        public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
        {
            int messageCount = 0;

            while (true)
			{
                var messageData = await ReadMessage();
                if (messageData.Count > 0)
                {
                    foreach(var element in messageData)
					{
                        await outputMessageStream.WriteAsync(element, 0, element.Length);
					}
                    messageCount++;
                }
                else
                    break;
            }

            return messageCount;
        }
    }
}


