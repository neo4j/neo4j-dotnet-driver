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

using System.Diagnostics.CodeAnalysis;

namespace Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;

internal interface IMessageDecoderProvider
{
    /// <summary>
    /// Tries to get the message decoder for the given struct tag.
    /// </summary>
    /// <param name="tag">The PackStream struct tag (e.g. 0x70 for SUCCESS).</param>
    /// <param name="decoder">The decoder if found; non-null when the method returns true.</param>
    /// <returns>True if a decoder was found; otherwise false. Caller should throw if false.</returns>
    bool TryGetDecoder(byte tag, [NotNullWhen(true)] out IMessageDecoder? decoder);
}
