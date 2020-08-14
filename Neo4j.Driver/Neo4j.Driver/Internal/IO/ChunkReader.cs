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
        private MemoryStream ChunkBuffer { get; set; }
        private long ChunkBufferRemaining { get { return ChunkBuffer.Length - ChunkBuffer.Position; } }

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


        /*private async Task PopulateChunkBufferAsync(int requiredSize = Constants.ChunkBufferSize)
		{
            //If we are expecting further data in the buffer but it isn't there, then attempt to get more from the network input stream
            if (ChunkBufferRemaining < requiredSize)
            {
                var data = new byte[Constants.ChunkBufferSize]; //We will read in batches of this many bytes.
                long storedPosition = ChunkBuffer.Position;
                int numBytesRead = 0;

                //We could be in the middle of reading from the chunk buffer, so store it's state and ensure we add to the end of it.
                ChunkBuffer.Position = ChunkBuffer.Length;

                while ((numBytesRead = await InputStream.ReadAsync(data, 0, Constants.ChunkBufferSize).ConfigureAwait(false)) > 0)
                {
                    ChunkBuffer.Write(data, 0, numBytesRead);

                    if (numBytesRead < requiredSize)   //If we've read everything that is available
                        break;
                    if (ChunkBufferRemaining >= requiredSize)  //if we have read enough to meet the required amount of data
                        break;                    
                }

                ChunkBuffer.Position = storedPosition;  //Restore the chunkbuffer state so that any reads can continue

                if (ChunkBuffer.Length == 0)  //No data so stop
                {
                    throw new IOException($"Unexpected end of stream, ChunkBuffer was not populated with any data");
                }
            }
        }*/

        private async Task PopulateChunkBufferAsync(int requiredSize = Constants.ChunkBufferSize)
        {
            if (ChunkBufferRemaining >= requiredSize)
                return;

            int bufferSize = Math.Max(Constants.ChunkBufferSize, requiredSize);
            var data = new byte[bufferSize]; //We will read in batches of this many bytes.
            long storedPosition = ChunkBuffer.Position;
            int numBytesRead = 0;

            while ((numBytesRead = await InputStream.ReadAsync(data, 0, bufferSize).ConfigureAwait(false)) > 0)
            {
                ChunkBuffer.Write(data, 0, numBytesRead);

                if (numBytesRead < bufferSize)
                    break;
            }

            ChunkBuffer.Position = storedPosition;  //Restore the chunkbuffer state so that any reads can continue

            if (ChunkBuffer.Length == 0)  //No data so stop
            {
                throw new IOException($"Unexpected end of stream, ChunkBuffer was not populated with any data");
            }
        }

        private async Task<byte[]> ReadDataOfSizeAsync(int requiredSize)
		{      
            await PopulateChunkBufferAsync(requiredSize).ConfigureAwait(false);

            var data = new byte[requiredSize];
            int readSize = ChunkBuffer.Read(data, 0, requiredSize);

            if (readSize != requiredSize)
                throw new IOException($"Unexpected end of stream, unable to read required data size");
            
            return data;
		}

        private async Task<bool> ConstructMessageAsync(Stream outputMessageStream)
        {
            int rawChunkDataSize = 0;
            while (ChunkBuffer.Position < ChunkBuffer.Length)   //There is data to dechunk
            {
                var chunkHeader = await ReadDataOfSizeAsync(ChunkHeaderSize).ConfigureAwait(false);
                var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeader);

                if (chunkSize == 0) //NOOP or end of message
                {
                    if(rawChunkDataSize > 0)    //We have been reading data so this is the end of a message
					    break;
					
                    continue;   //Its a NOOP so skip it
                }

                var rawChunkData = await ReadDataOfSizeAsync(chunkSize).ConfigureAwait(false);
                rawChunkDataSize = rawChunkData.Length;
                outputMessageStream.Write(rawChunkData, 0, chunkSize);    //Put the raw chunk data into the outputstream
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
                await PopulateChunkBufferAsync().ConfigureAwait(false);

                while (ChunkBuffer.Position < ChunkBuffer.Length)   //We have not finished parsing the chunkbuffer, so further messages to dechunk
                {
                    if (await ConstructMessageAsync(outputMessageStream).ConfigureAwait(false))
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


