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
        private Stream InputStream { get; set; }
        private ILogger Logger { get; set; }
        MemoryStream ChunkBuffer { get; set; }

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


        private async Task PopulateChunkBuffer()
		{  
            int numBytesRead = 0;
            var data = new byte[Constants.ChunkBufferSize]; //We will read in batches of this many bytes.

            //We could be in the middle of reading from the chunk buffer, so store it's state and ensure we add to the end of it.
            long storedPosition = ChunkBuffer.Position;     
            ChunkBuffer.Position = ChunkBuffer.Length;      

            while ((numBytesRead = await InputStream.ReadAsync(data, 0, Constants.ChunkBufferSize).ConfigureAwait(false)) > 0)
            {
                await ChunkBuffer.WriteAsync(data, 0, numBytesRead);

                if (numBytesRead < Constants.ChunkBufferSize)
                    break;
            }

            ChunkBuffer.Position = storedPosition;  //Restore the chunkbuffer state so that any reads can continue
        }
        
        private async Task<byte[]> ReadDataOfSize(int requiredSize)
		{
            //If we are expecting further data in the buffer but it isn't there, then attempt to get more from the network input buffer
            if (ChunkBuffer.Length - ChunkBuffer.Position < requiredSize)   
                await PopulateChunkBuffer();

            var data = new byte[requiredSize];
            int readSize = await ChunkBuffer.ReadAsync(data, 0, requiredSize).ConfigureAwait(false);

            if (readSize != requiredSize)
                throw new IOException($"Unexpected end of stream, unable to read required data size");
            
            return data;
		}

        private async Task<bool> ConstructMessage(Stream outputMessageStream)
        {
            int rawChunkDataSize = 0;
            while (ChunkBuffer.Position < ChunkBuffer.Length)   //There is data to dechunk
            {
                var chunkHeader = await ReadDataOfSize(ChunkHeaderSize);
                var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeader);

                if (chunkSize == 0) //NOOP or end of message
                {
                    if(rawChunkDataSize > 0)    //We have been reading data so this is the end of a message
					    break;
					
                    continue;   //Its a NOOP so skip it
                }

                var rawChunkData = await ReadDataOfSize(chunkSize);
                rawChunkDataSize = rawChunkData.Length;
                await outputMessageStream.WriteAsync(rawChunkData, 0, chunkSize);    //Put the raw chunk data into the outputstream
            }

            return (rawChunkDataSize > 0);    //Return if a message was constructed
                
        }

        public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
        {
            int messageCount = 0;
            //store output streams state, and ensure we add to the end of it.
            var previousStreamPosition = outputMessageStream.Position;
            outputMessageStream.Position = outputMessageStream.Length;

            using (ChunkBuffer = new MemoryStream())
            {
                ChunkBuffer.Position = 0;
                await PopulateChunkBuffer();

                while (ChunkBuffer.Position < ChunkBuffer.Length)   //We have not finished parsing the chunkbuffer, so further messages to dechunk
                {
                    if (await ConstructMessage(outputMessageStream))
                    {
                        messageCount++;
                    }
                }
            }

            //restore output streams state.
            outputMessageStream.Position = previousStreamPosition;
            return messageCount;
        }
    }
}


