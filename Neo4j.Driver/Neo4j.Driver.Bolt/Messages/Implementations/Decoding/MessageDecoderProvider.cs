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
using Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;

namespace Neo4j.Driver.Bolt.Messages.Implementations.Decoding;

internal class MessageDecoderProvider : IMessageDecoderProvider
{
    private readonly IReadOnlyDictionary<byte, IMessageDecoder> _decodersByTag;

    public MessageDecoderProvider(IEnumerable<IMessageDecoder> decoders)
    {
        var dict = new Dictionary<byte, IMessageDecoder>();
        foreach (var decoder in decoders)
        {
            dict[decoder.HandledTag] = decoder;
        }

        _decodersByTag = dict;
    }

    /// <inheritdoc />
    public bool TryGetDecoder(byte tag, [NotNullWhen(true)] out IMessageDecoder? decoder)
    {
        var found = _decodersByTag.TryGetValue(tag, out var d);
        decoder = d;
        return found;
    }
}
