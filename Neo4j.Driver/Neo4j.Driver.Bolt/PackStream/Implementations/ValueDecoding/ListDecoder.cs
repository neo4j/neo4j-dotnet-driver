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
using Neo4j.Driver.Bolt.PackStream.Abstractions;
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.ValueDecoderHelpers;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

/// <summary>
/// Decodes List values from PackStream format.
/// TinyList: marker 0x90-0x9F (count in low nibble)
/// List8: marker 0xD4 + 1 byte count
/// List16: marker 0xD5 + 2 byte big-endian count
/// List32: marker 0xD6 + 4 byte big-endian count
/// </summary>
internal class ListDecoder(ILogger logger) : SequenceDecoderBase(logger), IRecursiveValueDecoder
{
    private IPackStreamDecoder? _recursionDecoder;

    private static readonly byte[] TinyListMarkers = Enumerable.Range(0x90, 16).Select(i => (byte)i).ToArray();

    public override byte[] HandledMarkerBytes =>
        [..TinyListMarkers, PackStreamMarker.List8, PackStreamMarker.List16, PackStreamMarker.List32];

    private static IntegerSize GetIntSize(byte marker) => (IntegerSize)(marker - PackStreamMarker.List8);

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        _recursionDecoder = _recursionDecoder ?? throw new InvalidOperationException("Recursion decoder is not set.");

        var reader = new SequenceReader<byte>(buffer);
        var marker = ReadValidMarkerByte(ref reader);

        Logger.LogDebug("Decoding list with marker 0x{Marker:X2}", marker);

        var itemCount = marker switch
        {
            _ when (marker & 0xF0) == PackStreamMarker.TinyList => marker & 0x0F,
            PackStreamMarker.List8 or PackStreamMarker.List16 or PackStreamMarker.List32
                => ReadSize(ref reader, GetIntSize(marker)),
            _ => throw new InvalidOperationException($"Unknown list marker byte: 0x{marker:X2}")
        };

        var listItemsData = DecodePayload(ref reader, itemCount, _recursionDecoder);

        var value = PackStreamValueView.List(
            listItemsData,
            itemCount,
            _recursionDecoder);

        Logger.LogDebug("Decoded list: {ItemCount} items, {TotalBytes} total bytes", itemCount, listItemsData.Length);

        return new ValueDecoderResult(value, (int)reader.Consumed);
    }

    public void SetRecursionDecoder(IPackStreamDecoder decoder)
    {
        _recursionDecoder = decoder;
    }
}
