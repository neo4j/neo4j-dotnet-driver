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
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO;

//TODO: Optimize reading stream with Span/Memory in .net6+
internal sealed class ChunkReader
{
    private Stream InputStream { get; }
    private MemoryStream ChunkBuffer { get; set; }
    private long ChunkBufferRemaining => ChunkBuffer.Length - ChunkBuffer.Position;
    public int ReadTimeoutSeconds { get; set; }

    private const int ChunkHeaderSize = 2;
    private readonly int _readTimeoutMs = -1;

    internal ChunkReader(Stream downStream)
    {
        Throw.ArgumentNullException.IfNull(downStream, nameof(downStream));
        Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream));
        InputStream = downStream;
    }

    private void ChunkBufferTrimUsedData()
    {
        //Remove 'used' data from memory stream, that is everything before it's current position
        var internalBuffer = ChunkBuffer.GetBuffer();
        Buffer.BlockCopy(internalBuffer, (int)ChunkBuffer.Position, internalBuffer, 0, (int)ChunkBufferRemaining);
        ChunkBuffer.SetLength((int)ChunkBufferRemaining);
        ChunkBuffer.Position = 0;
    }

    private async Task PopulateChunkBufferAsync(int requiredSize = Constants.ChunkBufferSize)
    {
        if (ChunkBufferRemaining >= requiredSize)
            return;

        ChunkBufferTrimUsedData();

        var storedPosition = ChunkBuffer.Position;
        requiredSize -= (int)ChunkBufferRemaining;
        var bufferSize = Math.Max(Constants.ChunkBufferSize, requiredSize);
        var data = new byte[bufferSize];

        ChunkBuffer.Position = ChunkBuffer.Length;

        while (requiredSize > 0)
        {
            var numBytesRead = await InputStream
                .ReadWithTimeoutAsync(data, 0, bufferSize, _readTimeoutMs)
                .ConfigureAwait(false);

            if (numBytesRead <= 0)
                break;

            ChunkBuffer.Write(data, 0, numBytesRead);
            requiredSize -= numBytesRead;
        }

        ChunkBuffer.Position = storedPosition;  //Restore the chunk buffer state so that any reads can continue

        if (ChunkBuffer.Length == 0)  //No data so stop
        {
            throw new IOException("Unexpected end of stream, unable to read expected data from the network connection");
        }
    }

    private async Task<byte[]> ReadDataOfSizeAsync(int requiredSize)
    {
        await PopulateChunkBufferAsync(requiredSize).ConfigureAwait(false);

        var data = new byte[requiredSize];
        var readSize = ChunkBuffer.Read(data, 0, requiredSize);

        if (readSize != requiredSize)
            throw new IOException("Unexpected end of stream, unable to read required data size");

        return data;
    }

    private async Task<bool> ConstructMessageAsync(Stream outputMessageStream)
    {
        var dataRead = false;

        while (true)
        {
            var chunkHeader = await ReadDataOfSizeAsync(ChunkHeaderSize).ConfigureAwait(false);
            var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeader);

            if (chunkSize == 0) //NOOP or end of message
            {
                //We have been reading data so this is the end of a message zero chunk
                //Or there is no data remaining after this NOOP
                if (dataRead || ChunkBufferRemaining <= 0)
                    break;

                //Its a NOOP so skip it
                continue;
            }

            var rawChunkData = await ReadDataOfSizeAsync(chunkSize).ConfigureAwait(false);
            dataRead = true;
            outputMessageStream.Write(rawChunkData, 0, chunkSize);    //Put the raw chunk data into the output stream
        }

        return dataRead;    //Return if a message was constructed

    }

    public async Task<int> ReadNextMessagesAsync(Stream outputMessageStream)
    {
        var messageCount = 0;
        //store output streams state, and ensure we add to the end of it.
        var previousStreamPosition = outputMessageStream.Position;
        outputMessageStream.Position = outputMessageStream.Length;

        using (ChunkBuffer = new MemoryStream())
        {
            long chunkBufferPosition = -1;   //Use this as we need an initial state < ChunkBuffer.Length

            while (chunkBufferPosition < ChunkBuffer.Length)   //We have not finished parsing the chunk buffer, so further messages to de-chunk
            {
                if (await ConstructMessageAsync(outputMessageStream).ConfigureAwait(false))
                {
                    messageCount++;
                }

                chunkBufferPosition = ChunkBuffer.Position;
            }
        }

        //restore output streams state.
        outputMessageStream.Position = previousStreamPosition;
        return messageCount;
    }

}