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
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream.Implementations.ValueDecoding;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.Helpers;

public static class ValueDecoderHelpers
{
    public static int ReadSize(ref SequenceReader<byte> reader, ValueDecoderBase.IntegerSize integerSize)
    {
        return integerSize switch
        {
            ValueDecoderBase.IntegerSize.Byte => SequenceReaderHelper.ReadByte(ref reader),
            ValueDecoderBase.IntegerSize.Short => (ushort)SequenceReaderHelper.ReadShortBigEndian(ref reader),
            ValueDecoderBase.IntegerSize.Int => SequenceReaderHelper.ReadIntBigEndian(ref reader),
            _ => throw new ArgumentOutOfRangeException(nameof(integerSize), integerSize, null)
        };
    }

    public static long ReadInteger(ref SequenceReader<byte> reader, ValueDecoderBase.IntegerSize integerSize)
    {
        return integerSize switch
        {
            ValueDecoderBase.IntegerSize.Byte => (sbyte)SequenceReaderHelper.ReadByte(ref reader),
            ValueDecoderBase.IntegerSize.Short => SequenceReaderHelper.ReadShortBigEndian(ref reader),
            ValueDecoderBase.IntegerSize.Int => SequenceReaderHelper.ReadIntBigEndian(ref reader),
            ValueDecoderBase.IntegerSize.Long => SequenceReaderHelper.ReadLongBigEndian(ref reader),
            _ => throw new ArgumentOutOfRangeException(nameof(integerSize), integerSize, null)
        };
    }

    public static void EnsureBufferNotEmpty(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            throw new ProtocolException("Buffer is empty.");
        }
    }

    public static void EnsureReaderNotFinished(SequenceReader<byte> reader)
    {
        if (reader.End)
        {
            throw new ProtocolException("Unexpected end of buffer.");
        }
    }
}
