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
using System.Buffers.Binary;
using Neo4j.Driver;

namespace Neo4j.Driver.Bolt.PackStream.Implementations.Helpers;

/// <summary>
/// Read-or-throw helpers for <see cref="SequenceReader{Byte}"/>. All methods throw
/// <see cref="ProtocolException"/> with a generic message when the read would exceed the buffer.
/// </summary>
internal static class SequenceReaderHelper
{
    private const string BufferTooShortMessage = "Buffer too short.";

    public static byte ReadByte(ref SequenceReader<byte> reader)
    {
        return reader.TryRead(out var b)
            ? b
            : throw new ProtocolException(BufferTooShortMessage);
    }

    public static short ReadShortBigEndian(ref SequenceReader<byte> reader)
    {
        return reader.TryReadBigEndian(out short value)
            ? value
            : throw new ProtocolException(BufferTooShortMessage);
    }

    public static int ReadIntBigEndian(ref SequenceReader<byte> reader)
    {
        return reader.TryReadBigEndian(out int value)
            ? value
            : throw new ProtocolException(BufferTooShortMessage);
    }

    public static long ReadLongBigEndian(ref SequenceReader<byte> reader)
    {
        return reader.TryReadBigEndian(out long value)
            ? value
            : throw new ProtocolException(BufferTooShortMessage);
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes and returns them as a <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    public static ReadOnlySequence<byte> ReadExact(ref SequenceReader<byte> reader, int count)
    {
        return reader.TryReadExact(count, out var data)
            ? data
            : throw new ProtocolException(BufferTooShortMessage);
    }

    /// <summary>
    /// Reads 8 bytes big-endian as a double. Uses a stack-allocated buffer when the data spans segments.
    /// </summary>
    public static double ReadDouble(ref SequenceReader<byte> reader)
    {
        var buffer = ReadExact(ref reader, 8);
        if (buffer.IsSingleSegment)
        {
            return BinaryPrimitives.ReadDoubleBigEndian(buffer.FirstSpan);
        }

        Span<byte> temp = stackalloc byte[8];
        buffer.CopyTo(temp);
        return BinaryPrimitives.ReadDoubleBigEndian(temp);
    }
}
