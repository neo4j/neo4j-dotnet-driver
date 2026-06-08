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
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Abstractions;

internal interface IPackStreamDecoder
{
    /// <summary>
    /// Decodes values asynchronously from a byte reader. Yields one value per Bolt message body.
    /// The consumer stops enumerating when it has read enough; the reader position advances accordingly.
    /// Pass cancellation via <c>Decode(reader, ct)</c> or <c>Decode(reader).WithCancellation(ct)</c>.
    /// </summary>
    IAsyncEnumerable<PackStreamValueView> Decode(
        IByteReader byteReader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decodes a single value synchronously from an already-buffered sequence.
    /// Used for nested structures like lists and maps.
    /// </summary>
    ValueDecoderResult Decode(ReadOnlySequence<byte> buffer);
}
