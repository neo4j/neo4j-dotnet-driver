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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal sealed class PipelinedMessageReader : IMessageReader
{
    private readonly Stream _stream;
    private int _timeoutInMs;
    private CancellationTokenSource _source;
    private readonly Memory<byte> _headerMemory;
    private readonly PipeReader _pipeReader;

    internal PipelinedMessageReader(Stream inputStream, DriverContext context)
        : this(inputStream, context, inputStream.ReadTimeout)
    {
    }
    
    internal PipelinedMessageReader(Stream inputStream, DriverContext context, int timeout)
    {
        _timeoutInMs = timeout;
        _stream = inputStream;
        _source = new CancellationTokenSource();
        _headerMemory = new Memory<byte>(new byte[2]);
        _pipeReader = PipeReader.Create(_stream, context.Config.MessageReaderConfig.StreamPipeReaderOptions);
    }
    
    public ValueTask DisposeAsync()
    {
        _source.Dispose();
        return _pipeReader.CompleteAsync();
    }

    public async ValueTask ReadAsync(IResponsePipeline pipeline, MessageFormat format)
    {
        try
        {
            while (!pipeline.HasNoPendingMessages)
            {
                var message = await ReadNextMessage(format, _pipeReader).ConfigureAwait(false);
                if (message == null)
                {
                    // Noop message,
                    continue;
                }

                // TODO: Optimize messages and dispatching to avoid allocations.
                // Dispatch the message to the pipeline, which will handle it.
                message.Dispatch(pipeline);

                // If the message is a failure message the connection requires reset and subsequent messages can be
                // ignored.
                if (message is FailureMessage)
                {
                    break;
                }
            }

            // await pipeReader.CompleteAsync().ConfigureAwait(false);
        }
        catch (IOException io)
        {
            await _pipeReader.CompleteAsync().ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException canceledException)
        {
            // A timeout has occurred, close the connection.
            await _pipeReader.CompleteAsync(canceledException).ConfigureAwait(false);
            _stream.Close();
            throw new ConnectionReadTimeoutException("Failed to read message from server within the specified timeout.",
                canceledException);
        }
        catch (Exception ex)
        {
            await _pipeReader.CompleteAsync(ex).ConfigureAwait(false);
            // If the exception is a protocol exception, the connection requires reset and subsequent messages can be
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

    private async ValueTask<IResponseMessage?> ReadNextMessage(MessageFormat format, PipeReader pipeReader)
    {
        ResetCancellation();
        // Read Bolt protocol chunk header
        var readResult = await pipeReader.ReadAtLeastAsync(2, _source.Token).ConfigureAwait(false);
        if (readResult is { IsCompleted: true, Buffer.Length: < 2 })
        {
            throw new IOException("Unexpected end of stream, unable to read expected data from the network connection");
        }

        var lengthSlice = readResult.Buffer.Slice(0, 2);
        lengthSlice.CopyTo(_headerMemory.Span);
        var size = BinaryPrimitives.ReadUInt16BigEndian(_headerMemory.Span);
        
        // if the size is 0, it means the message was a noop message.
        if (size == 0)
        {
            // Advance the pipeReader to the next chunk
            pipeReader.AdvanceTo(readResult.Buffer.Slice(2).Start);
            return null;
        }

        // Read chunks storing the lengths of each chunk.
        // Because the length of the message is unknown until the end of writing allocating a single buffer to read the entire
        // message is not possible.
        var sizes = new List<ushort>(2);
        // The total size of the message is the sum of all the chunks that make up the message ignoring markers & headers.
        var totalSize = 0;
        do
        {
            sizes.Add(size);
            totalSize += size;
            // The minimumRead is the length of all previous chunk headers & data, plus the next chunk header.
            // If there is no next chunk it will read the end of the message marker.
            // Given a hypothetical message with 2 chunks, 6 bytes across the two chunks the total data is 12 bytes.
            // e.g. 0x00, 0x04 0x04, 0x03, 0x02, 0x01, 0x00, 0x02, 0x03, 0x04, 0x00, 0x00
            // 0x00, 0x04, <- First chunk header
            // 0x04, 0x03, 0x02, 0x01, <- First chunk body
            // 0x00, 0x02, <- Second chunk header
            // 0x03, 0x04, <- Second chunk body
            // 0x00, 0x00  <- end of message marker
            var minimumRead = totalSize + 2 * (sizes.Count + 1);

            // If the buffer is less than the minimum read size, read more data from the stream.
            if (readResult.Buffer.Length < minimumRead)
            {
                ResetCancellation();
                readResult = await pipeReader.ReadAtLeastAsync(minimumRead, _source.Token).ConfigureAwait(false);
                if (readResult.IsCompleted && readResult.Buffer.Length < minimumRead)
                {
                    throw new IOException(
                        "Unexpected end of stream, unable to read expected data from the network connection");
                }
            }

            // Read the chunk header, If the next chunk header is 0x00, 0x00 that marks the end of the message.
            var endOfChunk = readResult.Buffer.Slice(minimumRead - 2, 2);
            endOfChunk.CopyTo(_headerMemory.Span);
            size = BinaryPrimitives.ReadUInt16BigEndian(_headerMemory.Span);
        } while (size != 0);

        // If there is only one chunk and it is a single segment, we can just read it directly
        if (sizes.Count == 1)
        {
            var chunkBuffer = readResult.Buffer.Slice(2, totalSize + 2);
            if (chunkBuffer.IsSingleSegment)
            {
                return RawParse(format, pipeReader, totalSize, chunkBuffer);
            }
        }

        // Otherwise we need to copy the data into a single buffer to parse it.
        return CondenseChunksAndParse(format, pipeReader, totalSize, sizes, readResult);
    }

    private static IResponseMessage RawParse(
        MessageFormat format,
        PipeReader pipeReader,
        int size,
        ReadOnlySequence<byte> buffer)
    {
        var packStreamReader = new SpanPackStreamReader(
            format,
            buffer.First.Span.Slice(0, size));

        var message = packStreamReader.ReadMessage();
        // Advance to end of message by create a buffer slice from the end of the chunk.
        pipeReader.AdvanceTo(buffer.End);
        return message;
    }
    
    private static IResponseMessage CondenseChunksAndParse(
        MessageFormat format,
        PipeReader pipeReader,
        int totalSize,
        List<ushort> sizes,
        ReadResult readResult)
    {
        // Borrow memory from shared pool..
        using var memory = MemoryPool<byte>.Shared.Rent(totalSize);
        var span = memory.Memory.Span.Slice(0, totalSize);
        // Copy all chunks into span removing chunk headers.
        CopyToMemory(sizes, readResult, span, pipeReader);
        // allocate SpanReader over the span and parse the message.
        var packStreamReader = new SpanPackStreamReader(format, span);
        return packStreamReader.ReadMessage();
    }

    private static void CopyToMemory(List<ushort> sizes, ReadResult readResult, Span<byte> span, PipeReader pipeReader)
    {
        var memoryPosition = 0;
        var streamStart = 2;
        foreach (var chunkSize in sizes)
        {
            var chunk = readResult.Buffer.Slice(streamStart, chunkSize);
            chunk.CopyTo(span.Slice(memoryPosition, chunkSize));
            memoryPosition += chunkSize;
            streamStart += chunkSize + 2;
        }
        // Advance to end of message by create a buffer slice from the end of the chunk.
        pipeReader.AdvanceTo(readResult.Buffer.Slice(streamStart).Start);
    }

    public void SetReadTimeoutInMs(int ms)
    {
        _timeoutInMs = ms;
    }
}
