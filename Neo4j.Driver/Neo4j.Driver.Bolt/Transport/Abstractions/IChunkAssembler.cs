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

namespace Neo4j.Driver.Bolt.Transport.Abstractions;

/// <summary>
/// Assembles Bolt protocol chunks from a byte source into complete message payloads. Each yielded
/// <see cref="ReadOnlySequence{T}"/> is the concatenated body of all chunks for one message (chunk
/// headers and the end-of-message marker are consumed but not included). The sequence may be a
/// single segment when the message fit in one chunk, avoiding extra copies.
/// </summary>
public interface IChunkAssembler
{
    /// <summary>
    /// Reads from the given byte reader and yields one message payload per Bolt message.
    /// Each payload is the raw message bytes (PackStream) without chunk framing.
    /// </summary>
    /// <param name="byteReader">The byte reader to read from (e.g. from the network).</param>
    /// <param name="cancellationToken">Cancellation for the read operation.</param>
    /// <returns>An async sequence of message payloads.</returns>
    IAsyncEnumerable<ReadOnlySequence<byte>> ReadMessagesAsync(
        IByteReader byteReader,
        CancellationToken cancellationToken = default);
}
