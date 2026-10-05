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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Implementations;

internal class PackStreamDecoder : IPackStreamDecoder
{
    private readonly IChunkAssembler _chunkAssembler;
    private readonly IValueDecoderProvider _valueDecoderProvider;
    private readonly ILogger _logger;

    public PackStreamDecoder(
        IChunkAssembler chunkAssembler,
        IValueDecoderProvider valueDecoderProvider,
        ILogger logger)
    {
        _chunkAssembler = chunkAssembler ?? throw new ArgumentNullException(nameof(chunkAssembler));
        _valueDecoderProvider = valueDecoderProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            throw new InvalidOperationException("Buffer is empty. Cannot decode value.");
        }

        var markerByte = buffer.First.Span[0];

        if (!_valueDecoderProvider.TryGetDecoder(markerByte, this, out var decoder))
        {
            throw new InvalidOperationException($"Unknown marker byte: 0x{markerByte:X2}");
        }

        return decoder.Decode(buffer);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<PackStreamValueView> Decode(
        IByteReader byteReader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var count = 0;
        _logger.LogDebug("Beginning PackStream decode from stream");
        await foreach (var buffer in _chunkAssembler.ReadMessagesAsync(byteReader, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogTrace("Processing message chunk ({Bytes} bytes)", buffer.Length);
            var bufferPosition = 0;
            while (bufferPosition < buffer.Length)
            {
                var remaining = buffer.Slice(bufferPosition);
                var result = Decode(remaining);
                _logger.LogTrace(
                    "Decoded value: {Value} (consumed {BytesConsumed} bytes)",
                    result.Value,
                    result.BytesConsumed);

                bufferPosition += result.BytesConsumed;
                count++;
                yield return result.Value;
            }
        }

        _logger.LogDebug("PackStream decode ended: {Count} values decoded", count);
    }
}
