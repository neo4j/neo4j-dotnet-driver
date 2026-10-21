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

internal class BooleanDecoder(ILogger logger) : ValueDecoderBase(logger)
{
    public override byte[] HandledMarkerBytes => [PackStreamMarker.True, PackStreamMarker.False];

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        var marker = ReadValidMarkerByte(ref reader);
        
        var value = marker switch
        {
            PackStreamMarker.True => PackStreamValueView.Boolean(true),
            PackStreamMarker.False => PackStreamValueView.Boolean(false),
            _ => throw new InvalidOperationException($"Unknown marker byte: 0x{buffer.FirstSpan[0]:X2}")
        };

        Logger.LogDebug("Decoded boolean {Value} (1 byte)", value.BooleanValue);
        return new ValueDecoderResult(value, (int)reader.Consumed);
    }
}
