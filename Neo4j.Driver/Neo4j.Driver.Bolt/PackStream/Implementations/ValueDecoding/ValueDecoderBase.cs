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
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.SequenceReaderHelper;
using static Neo4j.Driver.Bolt.PackStream.Implementations.Helpers.ValueDecoderHelpers;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

public abstract class ValueDecoderBase(ILogger logger) : IValueDecoder
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract byte[] HandledMarkerBytes { get; }

    public abstract ValueDecoderResult Decode(ReadOnlySequence<byte> buffer);

    public enum IntegerSize
    {
        Byte = 0,
        Short = 1,
        Int = 2,
        Long = 3,
    }

    public virtual bool IsMarkerByteHandled(byte markerByte) => HandledMarkerBytes.Contains(markerByte);

    protected byte ReadValidMarkerByte(ref SequenceReader<byte> reader)
    {
        EnsureReaderNotFinished(reader);
        var markerByte = ReadByte(ref reader);
        EnsureMarkerByteValid(markerByte);
        return markerByte;
    }

    protected void EnsureMarkerByteValid(byte markerByte)
    {
        if (!IsMarkerByteHandled(markerByte))
        {
            Logger.LogTrace("Unknown marker byte 0x{Marker:X2}", markerByte);
            throw new InvalidOperationException($"Unknown marker byte: 0x{markerByte:X2}");
        }
    }
}
