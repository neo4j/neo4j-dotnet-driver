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

using Neo4j.Driver.Bolt.Messages.Types;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;

namespace Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;

internal interface IMessageDecoder
{
    /// <summary>
    /// The PackStream struct tag byte this decoder handles (e.g. 0x70 for SUCCESS).
    /// </summary>
    byte HandledTag { get; }

    /// <summary>
    /// Decodes the struct payload into a <see cref="BoltResponseMessage" />.
    /// </summary>
    BoltResponseMessage Decode(PackStreamStructView structView);
}
