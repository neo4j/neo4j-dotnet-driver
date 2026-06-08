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
/// Decodes Map (Dictionary) values from PackStream format.
/// TinyMap: marker 0xA0-0xAF (entry count in low nibble)
/// Map8: marker 0xD8 + 1 byte count
/// Map16: marker 0xD9 + 2 byte big-endian count
/// Map32: marker 0xDA + 4 byte big-endian count
/// Payload is [key, value, key, value, ...] for each entry.
/// </summary>
internal class MapDecoder(ILogger logger) : SequenceDecoderBase(logger), IRecursiveValueDecoder
{
    private IPackStreamDecoder? _recursionDecoder;

    private static readonly byte[] TinyMapMarkers = Enumerable.Range(0xA0, 16).Select(i => (byte)i).ToArray();

    public override byte[] HandledMarkerBytes =>
        [..TinyMapMarkers, PackStreamMarker.Map8, PackStreamMarker.Map16, PackStreamMarker.Map32];

    public override bool IsMarkerByteHandled(byte markerByte)
    {
        return (markerByte & 0xF0) == PackStreamMarker.TinyMap
            || markerByte is PackStreamMarker.Map8 or PackStreamMarker.Map16 or PackStreamMarker.Map32;
    }

    private static IntegerSize GetIntSize(byte marker) => (IntegerSize)(marker - PackStreamMarker.Map8);

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        _recursionDecoder = _recursionDecoder ?? throw new InvalidOperationException("Recursion decoder is not set.");

        var reader = new SequenceReader<byte>(buffer);
        var marker = ReadValidMarkerByte(ref reader);

        Logger.LogDebug("Decoding map with marker 0x{Marker:X2}", marker);

        var entryCount = marker switch
        {
            _ when (marker & 0xF0) == PackStreamMarker.TinyMap => marker & 0x0F, 
            
            PackStreamMarker.Map8 or PackStreamMarker.Map16 or PackStreamMarker.Map32
                => ReadSize(ref reader, GetIntSize(marker)),
            
            _ => throw new InvalidOperationException($"Unknown map marker byte: 0x{marker:X2}")
        };

        var entriesData = DecodePayload(ref reader, 2 * entryCount, _recursionDecoder);

        var value = PackStreamValueView.Map(
            entriesData,
            entryCount,
            _recursionDecoder);

        Logger.LogDebug("Decoded map: {EntryCount} entries, {TotalBytes} total bytes", entryCount, entriesData.Length);

        return new ValueDecoderResult(value, (int)reader.Consumed);
    }

    public void SetRecursionDecoder(IPackStreamDecoder decoder)
    {
        _recursionDecoder = decoder;
    }
}
