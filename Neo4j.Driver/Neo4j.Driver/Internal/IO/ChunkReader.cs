// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Helpers;

namespace Neo4j.Driver.Internal.IO;

//TODO: Optimize reading stream with Span/Memory in .net6+

internal sealed class ChunkReader : IChunkReader
{
    private const int ChunkHeaderSize = 2;
    private int _readTimeoutMs = -1;

    internal ChunkReader(Stream networkStream)
    {
        NetworkStream = networkStream ?? throw new ArgumentNullException(nameof(networkStream));
        Throw.ArgumentOutOfRangeException.IfFalse(networkStream.CanRead, nameof(networkStream.CanRead));
    }

    private Stream NetworkStream { get; }
    private MemoryStream ChunkBuffer { get; set; }
    private long ChunkBufferRemaining => ChunkBuffer.Length - ChunkBuffer.Position;

    public async Task<int> ReadMessageChunksToBufferStreamAsync(Stream bufferStream)
    {
        var messageCount = 0;
        //store output streams state, and ensure we add to the end of it.
        var previousStreamPosition = bufferStream.Position;
        bufferStream.Position = bufferStream.Length;

        using (ChunkBuffer = new MemoryStream())
        {
            //Use this as we need an initial state < ChunkBuffer.Length
            long chunkBufferPosition = -1;

            //We have not finished parsing the chunk buffer, so further messages to de-chunk
            while (chunkBufferPosition < ChunkBuffer.Length)
            {
                if (await ConstructMessageAsync(bufferStream).ConfigureAwait(false))
                {
                    messageCount++;
                }

                chunkBufferPosition = ChunkBuffer.Position;
            }
        }

        //restore output streams state.
        bufferStream.Position = previousStreamPosition;
        return messageCount;
    }

    public void SetTimeoutInMs(int ms)
    {
        _readTimeoutMs = ms;
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
        {
            return;
        }

        ChunkBufferTrimUsedData();

        var storedPosition = ChunkBuffer.Position;
        requiredSize -= (int)ChunkBufferRemaining;
        var bufferSize = Math.Max(Constants.ChunkBufferSize, requiredSize);
        var data = new byte[bufferSize];

        ChunkBuffer.Position = ChunkBuffer.Length;

        while (requiredSize > 0)
        {
            var numBytesRead = await NetworkStream
                .ReadWithTimeoutAsync(data, 0, bufferSize, _readTimeoutMs)
                .ConfigureAwait(false);

            if (numBytesRead <= 0)
            {
                break;
            }

            ChunkBuffer.Write(data, 0, numBytesRead);
            requiredSize -= numBytesRead;
        }

        //Restore the chunk buffer state so that any reads can continue
        ChunkBuffer.Position = storedPosition;

        //No data so stop
        if (ChunkBuffer.Length == 0)
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
        {
            throw new IOException("Unexpected end of stream, unable to read required data size");
        }

        return data;
    }

    private async Task<bool> ConstructMessageAsync(Stream outputMessageStream)
    {
        var dataRead = false;

        while (true)
        {
            var chunkHeader = await ReadDataOfSizeAsync(ChunkHeaderSize).ConfigureAwait(false);
            var chunkSize = PackStreamBitConverter.ToUInt16(chunkHeader);

            //NOOP or end of message
            if (chunkSize == 0)
            {
                //We have been reading data so this is the end of a message zero chunk
                //Or there is no data remaining after this NOOP
                if (dataRead || ChunkBufferRemaining <= 0)
                {
                    break;
                }

                //Its a NOOP so skip it
                continue;
            }

            var rawChunkData = await ReadDataOfSizeAsync(chunkSize).ConfigureAwait(false);
            dataRead = true;
            //Put the raw chunk data into the output stream
            outputMessageStream.Write(rawChunkData, 0, chunkSize);
        }

        //Return if a message was constructed
        return dataRead;
    }
}
