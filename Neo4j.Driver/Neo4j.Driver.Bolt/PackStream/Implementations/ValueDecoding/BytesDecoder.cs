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
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.SequenceReaderHelper;
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.ValueDecoderHelpers;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

/// <summary>
/// Decodes Bytes values from PackStream format.
/// Bytes8: marker (0xCC) + 1 byte length + data
/// Bytes16: marker (0xCD) + 2 byte big-endian length + data
/// Bytes32: marker (0xCE) + 4 byte big-endian length + data
/// </summary>
internal class BytesDecoder(ILogger logger) : ValueDecoderBase(logger)
{
    public override byte[] HandledMarkerBytes =>
        [PackStreamMarker.Bytes8, PackStreamMarker.Bytes16, PackStreamMarker.Bytes32];

    private static IntegerSize GetIntSize(byte marker) => (IntegerSize)(marker - PackStreamMarker.Bytes8);

    public override ValueDecoderResult Decode(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        var marker = ReadValidMarkerByte(ref reader);
        var intSize = GetIntSize(marker);
        var length = ReadSize(ref reader, intSize);
        var bytesData = ReadExact(ref reader, length);
        var consumed = (int)reader.Consumed;
        Logger.LogDebug("Decoded bytes length {Length} ({BytesConsumed} bytes)", length, consumed);
        var value = PackStreamValueView.Bytes(bytesData);
        return new ValueDecoderResult(value, consumed);
    }
}
