// Copyright (c) "Neo4j"
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

#nullable enable
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal sealed class PipelinedMessageReader : IMessageReader
{
    private readonly Stream _clientReaderStream;
    private int _timeoutInMs;
    private CancellationTokenSource _source;

    private const int MaxChunkSize = 65_535;

    public PipelinedMessageReader(ITcpSocketClient socketClient, ILogger logger)
    {
        _timeoutInMs = socketClient.ReaderStream.ReadTimeout;
        _clientReaderStream = socketClient.ReaderStream;
        _source = new CancellationTokenSource();
    }
    
    ~PipelinedMessageReader()
    {
        _source.Dispose();
    }

    public async Task ReadAsync(IResponsePipeline pipeline, PackStreamReader reader)
    {
        var pipeReader = PipeReader.Create(_clientReaderStream, 
            new StreamPipeReaderOptions(leaveOpen: true, bufferSize: MaxChunkSize + 4));
        try
        {
            while (!pipeline.HasNoPendingMessages)
            {
                // Read Message
                var message = await ReadNextMessage(reader, pipeReader);
                if (message == null)
                {
                    continue;
                }

                message.Dispatch(pipeline);

                // If the message is a failure message, we should stop reading.
                if (message is FailureMessage)
                {
                    break;
                }
            }

            await pipeReader.CompleteAsync();
        }
        catch (OperationCanceledException canceledException)
        {
            await pipeReader.CompleteAsync(canceledException);
            _clientReaderStream.Close();
            throw new ConnectionReadTimeoutException("Failed to read message from server within the specified timeout.",
                canceledException);
        }
        catch (Exception ex)
        {
            await pipeReader.CompleteAsync(ex);
            throw;
        }
    }

    private void ResetCancellation()
    {
        if (_timeoutInMs <= 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        if (!_source.TryReset())
        {
            _source.Dispose();
            _source = new CancellationTokenSource();
        }
#else
        if (_source.IsCancellationRequested)
        {
            _source.Dispose();
            _source = new CancellationTokenSource();
        }
#endif
        _source.CancelAfter(_timeoutInMs);
    }

    private async Task<IResponseMessage?> ReadNextMessage(PackStreamReader reader, PipeReader pipeReader)
    {
        ResetCancellation();
        var headerMemory = new Memory<byte>(reader._buffers.LongBuffer).Slice(0, 2);
        // Read Bolt protocol chunk header
        var readResult = await pipeReader.ReadAtLeastAsync(2, _source.Token);
        if (readResult.IsCompleted && readResult.Buffer.Length < 2)
        {
            throw new IOException("Unexpected end of stream, unable to read expected data from the network connection");
        }
        var lengthSlice = readResult.Buffer.Slice(0, 2);
        
        lengthSlice.CopyTo(headerMemory.Span);
        
        var size = BinaryPrimitives.ReadInt16BigEndian(headerMemory.Span);
        // if the size is 0, it means the message was a noop message.
        if (size == 0)
        {
            // Advance the pipeReader to the next chunk
            pipeReader.AdvanceTo(readResult.Buffer.Slice(2).Start);
            return null;
        }

        // Read chunk data storing the lengths of each chunk in a list so we can construct multi chunk messages.
        // Because we don't know the length of the message we can't allocate a single buffer to read the entire message
        // ahead of the reads.
        var sizes = new List<short>(2);
        // The Total size of the message is the sum of all the chunk sizes for quickly calculating the minimum read size.
        var totalSize = 0;
        while (size != 0)
        {
            sizes.Add(size);
            totalSize += size;
            // the minimumRead is the length of all previous chunk headers & data, plus the next chunk header.
            // if there is no next chunk it will read the end of the message marker.
            // Given a hypothetical message with 2 chunks, 6 bytes across the two chunks the total data is 12 bytes.
            // e.g. 0x00, 0x04 0x04, 0x03, 0x02, 0x01, 0x00, 0x02, 0x03, 0x04, 0x00, 0x00
            // 0x00, 0x04, <- First chunk header
            // 0x04, 0x03, 0x02, 0x01, <- First chunk body
            // 0x00, 0x02, <- Second chunk header
            // 0x03, 0x04, <- Second chunk body
            // 0x00, 0x00  <- end of message marker
            var minimumRead = totalSize + 2 * (sizes.Count + 1);

            // if we have all the data needed for this chunk we can skip the read as it was already read to the buffer.
            if (readResult.Buffer.Length < minimumRead)
            {
                ResetCancellation();
                readResult = await pipeReader.ReadAtLeastAsync(minimumRead, _source.Token);
                if (readResult.IsCompleted && readResult.Buffer.Length < minimumRead)
                {
                    throw new IOException(
                        "Unexpected end of stream, unable to read expected data from the network connection");
                }
            }
            
            // Read the chunk header,
            // if the next chunk header is 0x00, 0x00 then it means that we have read the last chunk of the message.
            var endOfChunk = readResult.Buffer.Slice(minimumRead - 2, 2);
            endOfChunk.CopyTo(headerMemory.Span);
            size = BinaryPrimitives.ReadInt16BigEndian(headerMemory.Span);
        }

        // If there is only one chunk and it is a single segment, we can just read it directly
        if (sizes.Count == 1 && readResult.Buffer.Slice(2, totalSize).IsSingleSegment)
        {
            return RawParse(reader, pipeReader, readResult, totalSize);
        }
        // Otherwise we need to copy the data into a single buffer to parse it.
        return CondenseChunksAndParse(reader, pipeReader, totalSize, sizes, readResult);
    }

    private static IResponseMessage RawParse(
        PackStreamReader reader,
        PipeReader pipeReader,
        ReadResult readResult,
        int size)
    {
        var packStreamReader = new SpanPackStreamReader(
            reader._format,
            readResult.Buffer.FirstSpan.Slice(2, size));

        var end = size + 4;
        // Advance to end of message by create a buffer slice from the end of the chunk.
        pipeReader.AdvanceTo(readResult.Buffer.Slice(end).Start);
        return packStreamReader.ReadMessage();
    }
    
    private static IResponseMessage CondenseChunksAndParse(
        PackStreamReader reader,
        PipeReader pipeReader,
        int totalSize,
        List<short> sizes,
        ReadResult readResult)
    {
        // Borrow memory from shared pool..
        using var memory = MemoryPool<byte>.Shared.Rent(totalSize);
        // Copy all chunks into memory
        CopyToMemory(sizes, readResult, memory, pipeReader);
        // convert memory to array
        var bytes = memory.Memory.Span.Slice(0, totalSize);
        // Create a new stream from the array and parse it
        var packStreamReader = new SpanPackStreamReader(
            reader._format,
            bytes);
        
        return packStreamReader.ReadMessage();
    }

    private static void CopyToMemory(List<short> sizes, ReadResult readResult, IMemoryOwner<byte> memory, PipeReader pipeReader)
    {
        var memoryPosition = 0;
        var streamStart = 2;

        foreach (var chunkSize in sizes)
        {
            var chunk = readResult.Buffer.Slice(streamStart, chunkSize);
            chunk.CopyTo(memory.Memory.Span.Slice(memoryPosition, chunkSize));
            memoryPosition += chunkSize;
            streamStart = streamStart + chunkSize + 2;
        }
        // Advance to end of message by create a buffer slice from the end of the chunk.
        pipeReader.AdvanceTo(readResult.Buffer.Slice(streamStart).Start);
    }

    private static ReadOnlySequence<byte> ToSequence(
        List<short> sizes,
        ReadResult readResult,
        IMemoryOwner<byte> memory,
        PipeReader pipeReader)
    {
        var memoryPosition = 0;
        var streamStart = 2;
        
        bool initial = true;
        ReadOnlySequence<byte> sequence = default;

        foreach (var chunkSize in sizes)
        {
            var chunk = readResult.Buffer.Slice(streamStart, chunkSize);
            if (initial)
            {
                sequence = chunk;
            }
            else
            {
                sequence = new ReadOnlySequence<byte>(sequence., 0, chunk, 0);
            }
            
            memoryPosition += chunkSize;
            streamStart = streamStart + chunkSize + 2;
        }

        // Advance to end of message by create a buffer slice from the end of the chunk.
        pipeReader.AdvanceTo(readResult.Buffer.Slice(streamStart).Start);
    }

    public void SetReadTimeoutInMs(int ms)
    {
        _timeoutInMs = ms;
    }
}
