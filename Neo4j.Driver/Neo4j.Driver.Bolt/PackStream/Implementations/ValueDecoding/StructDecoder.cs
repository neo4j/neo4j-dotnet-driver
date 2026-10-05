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
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.SequenceReaderHelper;
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.ValueDecoderHelpers;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

/// <summary>
/// Decodes Structure values from PackStream format.
/// TinyStruct: marker 0xB0-0xBF (field count in low nibble)
/// Struct8: marker 0xDC + 1 byte count
/// Struct16: marker 0xDD + 2 byte big-endian count
/// Followed by tag byte then N field values.
/// </summary>
internal class StructDecoder(ILogger logger) : SequenceDecoderBase(logger), IRecursiveValueDecoder
{
    private IPackStreamDecoder? _recursionDecoder;

    private static readonly byte[] TinyStructMarkers = Enumerable.Range(0xB0, 16).Select(i => (byte)i).ToArray();

    public override byte[] HandledMarkerBytes =>
        [..TinyStructMarkers, PackStreamMarker.Struct8, PackStreamMarker.Struct16];     
    
    private static IntegerSize GetIntSize(byte marker) => (IntegerSize)(marker - PackStreamMarker.Struct8);

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        _recursionDecoder = _recursionDecoder ?? throw new InvalidOperationException("Recursion decoder is not set.");

        var reader = new SequenceReader<byte>(buffer);
        var marker = ReadValidMarkerByte(ref reader);

        Logger.LogDebug("Decoding struct with marker 0x{Marker:X2}", marker);

        var fieldCount = marker switch
        {
            // tiny struct
            _ when (marker & 0xF0) == PackStreamMarker.TinyStruct => marker & 0x0F,
            
            // 8 or 16-bit length indicator
            PackStreamMarker.Struct8 or PackStreamMarker.Struct16
                => ReadSize(ref reader, GetIntSize(marker)),
            
            _ => throw new InvalidOperationException($"Unknown struct marker byte: 0x{marker:X2}")
        };

        var tag = ReadByte(ref reader);

        var fieldsData = DecodePayload(ref reader, fieldCount, _recursionDecoder);
        var fields = new PackStreamListView(fieldsData, fieldCount, _recursionDecoder);
        var value = PackStreamValueView.Struct(new PackStreamStructView(tag, fields));

        Logger.LogDebug("Decoded struct: tag 0x{Tag:X2}, {FieldCount} fields, {TotalBytes} total bytes", tag, fieldCount, (int)reader.Consumed);

        return new ValueDecoderResult(value, (int)reader.Consumed);
    }

    public void SetRecursionDecoder(IPackStreamDecoder decoder)
    {
        _recursionDecoder = decoder;
    }
}
