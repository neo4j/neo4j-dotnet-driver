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
using Neo4j.Driver.Bolt.PackStream.Abstractions.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Types.ValueDecoding;
using Neo4j.Driver.Bolt.PackStream.Ephemeral;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

public class TinyIntDecoder(ILogger logger) : ValueDecoderBase(logger)
{
    private static readonly IEnumerable<byte> PositiveTinyIntMarkers =
        Enumerable.Range(0x00, 0x80).Select(i => (byte)i).ToArray();

    private static readonly IEnumerable<byte> NegativeTinyIntMarkers =
        Enumerable.Range(0xF0, 0x10).Select(i => (byte)i).ToArray();

    public override byte[] HandledMarkerBytes => [..PositiveTinyIntMarkers, ..NegativeTinyIntMarkers];

    public override bool IsMarkerByteHandled(byte markerByte)
    {
        var highNibble = (markerByte & 0xF0) >> 4;
        return highNibble is < 0x8 or 0xF;
    }

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        var marker = buffer.FirstSpan[0];

        var value = marker switch
        {
            <= 0x7F => marker, // positive tiny int
            >= 0xF0 => marker - 256, // negative tiny int
            _ => throw new InvalidOperationException($"Unknown marker byte: 0x{marker:X2}")
        };

        Logger.LogDebug("Decoded tiny int {Value} (1 byte)", value);
        return new ValueDecoderResult(PackStreamValueView.Integer(value), 1);
    }
    
}
