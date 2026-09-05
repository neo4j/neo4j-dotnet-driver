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
using Microsoft.Extensions.Logging;
using Neo4j.Driver.Bolt.Extensions;
using Neo4j.Driver.Bolt.PackStream.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

/// <summary>
/// Base for decoders that read a sequence of nested PackStream values (e.g. List, Map).
/// Provides a single-pass <see cref="DecodePayload"/> that advances the reader and returns
/// the payload as a <see cref="ReadOnlySequence{T}"/>.
/// </summary>
internal abstract class SequenceDecoderBase(ILogger logger) : ValueDecoderBase(logger)
{
    /// <summary>
    /// Decodes <paramref name="valueCount"/> PackStream values from the reader's current position,
    /// advancing the reader, and returns the slice of the buffer containing those values.
    /// </summary>
    protected ReadOnlySequence<byte> DecodePayload(
        ref SequenceReader<byte> reader,
        int valueCount,
        IPackStreamDecoder decoder)
    {
        var payloadStart = reader.UnreadSequence;
        var totalBytes = 0;

        for (var i = 0; i < valueCount; i++)
        {
            ProtocolException.ThrowIf(reader.UnreadSequence.IsEmpty);

            var result = decoder.Decode(reader.UnreadSequence);
            var bytesConsumed = result.BytesConsumed;
            totalBytes += bytesConsumed;
            reader.Advance(bytesConsumed);
        }

        return payloadStart.Slice(0, totalBytes);
    }
}
