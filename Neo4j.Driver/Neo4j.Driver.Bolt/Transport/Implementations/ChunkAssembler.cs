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

using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.Bolt.Extensions;
using Neo4j.Driver.Bolt.Transport.Abstractions;

using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.SequenceReaderHelper;

namespace Neo4j.Driver.Bolt.Transport.Implementations;

public class ChunkAssembler : IChunkAssembler
{
    private readonly ILogger _logger;

    public ChunkAssembler(ILogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<ReadOnlySequence<byte>> ReadMessagesAsync(
        IByteReader byteReader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        SequencePosition? pendingConsumed = null;
        ReadOnlySequence<byte> buffer = default;

        _logger.LogDebug("Beginning message read loop");

        while (true)
        {
            if (pendingConsumed.HasValue)
            {
                _logger.LogDebug("Advancing byte reader past previous message");
                byteReader.AdvanceTo(pendingConsumed.Value, buffer.End);
                pendingConsumed = null;
            }

            _logger.LogTrace("Reading from byte reader");
            var readResult = await byteReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            buffer = readResult.Buffer;

            if (buffer.IsEmpty && readResult.IsCompleted)
            {
                _logger.LogDebug("Byte reader completed without any data");
                break;
            }

            var consumed = buffer.Start;

            while (!consumed.Equals(buffer.End) &&
                   TryReadMessageFromBuffer(buffer, consumed, out var message, readResult.IsCompleted))
            {
                yield return message;

                // Advance consumed past header + body
                _logger.LogTrace("Advancing consumed past message header and body");
                consumed = buffer.GetPosition(sizeof(short) + message.Length, consumed);
            }

            pendingConsumed = consumed;

            if (!readResult.IsCompleted)
            {
                _logger.LogTrace("Byte reader not completed yet, waiting for more data");
                continue;
            }

            _logger.LogDebug("Byte reader completed");
            break;
        }

        if (pendingConsumed.HasValue)
        {
            _logger.LogTrace("Advancing byte reader past final message");
            byteReader.AdvanceTo(pendingConsumed.Value, buffer.End);
        }
    }

    private bool TryReadMessageFromBuffer(
        ReadOnlySequence<byte> buffer,
        SequencePosition consumed,
        out ReadOnlySequence<byte> message,
        bool readResultIsCompleted)
    {
        message = default;
        var seqReader = new SequenceReader<byte>(buffer.Slice(consumed));

        // Need header?
        if (seqReader.Remaining < sizeof(short))
        {
            ProtocolException.ThrowIf(
                readResultIsCompleted,
                () => new("Reached end of stream while reading message header"));

            _logger.LogTrace("Not enough data to read message header, waiting for more data");
            return false;
        }

        _logger.LogTrace("Reading message header");
        var messageSize = ReadShortBigEndian(ref seqReader);

        // Need body?
        if (seqReader.Remaining < messageSize)
        {
            ProtocolException.ThrowIf(
                readResultIsCompleted,
                () => new("Reached end of stream while reading message content"));

            _logger.LogTrace("Not enough data to read message body, waiting for more data");
            return false;
        }

        message = ReadExact(ref seqReader, messageSize);

        // message is complete, yield it
        _logger.LogDebug("Read complete message of size {MessageSize} bytes", message.Length);
        return true;
    }
}
