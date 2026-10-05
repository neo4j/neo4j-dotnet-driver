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

using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.Handshake;

/// <summary>
/// Performs Bolt protocol negotiation immediately after the TCP connection is established.
/// </summary>
internal interface IBoltHandshake
{
    /// <summary>
    /// Sends client version offers and returns the agreed protocol version for legacy handshakes.
    /// Manifest-style negotiation (server major 255) is not implemented yet.
    /// </summary>
    ValueTask<BoltHandshakeVersion> NegotiateAsync(
        IByteWriter writer,
        IByteReader reader,
        CancellationToken cancellationToken = default);
}
