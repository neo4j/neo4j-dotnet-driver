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

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.Bolt.Messages.Types;
using Neo4j.Driver.Bolt.Messages.Abstractions.Decoding;
using Neo4j.Driver.Bolt.PackStream;
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.Transport.Abstractions;

namespace Neo4j.Driver.Bolt.Messages.Implementations;

internal class BoltStreamDecoder
{
    private readonly IPackStreamDecoder _packStreamDecoder;
    private readonly IMessageDecoderProvider _messageDecoderProvider;
    private readonly ILogger _logger;

    public BoltStreamDecoder(
        IPackStreamDecoder packStreamDecoder,
        IMessageDecoderProvider messageDecoderProvider,
        ILogger logger)
    {
        _packStreamDecoder = packStreamDecoder ?? throw new ArgumentNullException(nameof(packStreamDecoder));
        _messageDecoderProvider = messageDecoderProvider ?? throw new ArgumentNullException(nameof(messageDecoderProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads Bolt messages from the byte stream. Each yielded value is a struct decoded as a Bolt message.
    /// </summary>
    public async IAsyncEnumerable<BoltResponseMessage> ReadMessagesAsync(
        IByteReader byteReader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var count = 0;
        _logger.LogDebug("Beginning Bolt message decode from stream");

        await foreach (var value in _packStreamDecoder.Decode(byteReader, cancellationToken).ConfigureAwait(false))
        {
            if (value.Type != PackStreamType.Struct)
            {
                throw new InvalidOperationException(
                    $"Expected a PackStream struct (Bolt message); got {value.Type}.");
            }

            var structView = value.StructValue;
            if (!_messageDecoderProvider.TryGetDecoder(structView.Tag, out var decoder))
            {
                throw new KeyNotFoundException(
                    $"No message decoder registered for tag 0x{structView.Tag:X2}.");
            }

            var message = decoder.Decode(structView);
            _logger.LogTrace("Decoded Bolt message: {Kind}", message.Kind);
            count++;
            yield return message;
        }

        _logger.LogDebug("Bolt message decode ended: {Count} messages decoded", count);
    }
}
