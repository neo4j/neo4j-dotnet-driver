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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Neo4j.Driver;
using Neo4j.Driver.Bolt.PackStream.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Implementations;

internal class PackStreamWriter(IBufferWriter<byte> writer) : IPackStreamWriter
{
    private readonly IBufferWriter<byte> _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public void WriteNull() => WriteByte(PackStreamMarker.Null);

    public void WriteBoolean(bool value) => WriteByte(value ? PackStreamMarker.True : PackStreamMarker.False);

    public void WriteInteger(long value)
    {
        // Tiny form: single byte, no marker; INT8 marker: -128..-17.
        switch (value)
        {
            case >= PackStreamInt.TinyIntegerMin and <= PackStreamInt.MaxInt8Value:
                WriteByte((byte)value);
                break;
            case >= PackStreamInt.MinInt8Value and <= PackStreamInt.Int8MarkerMax:
                WriteByte(PackStreamMarker.Int8);
                WriteByte((byte)value);
                break;
            case >= PackStreamInt.MinInt16Value and <= PackStreamInt.MaxInt16Value:
                WriteByte(PackStreamMarker.Int16);
                WriteBigEndian((short)value);
                break;
            case >= PackStreamInt.MinInt32Value and <= PackStreamInt.MaxInt32Value:
                WriteByte(PackStreamMarker.Int32);
                WriteBigEndian((int)value);
                break;
            default:
                WriteByte(PackStreamMarker.Int64);
                WriteBigEndian(value);
                break;
        }
    }

    public void WriteFloat64(double value)
    {
        WriteByte(PackStreamMarker.Float64);
        var span = _writer.GetSpan(8);
        BinaryPrimitives.WriteDoubleBigEndian(span, value);
        _writer.Advance(8);
    }

    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteStringHeader(byteCount);
        if (byteCount == 0)
        {
            return;
        }

        if (byteCount <= 512)
        {
            Span<byte> buf = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(value, buf);
            WriteRaw(buf);
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(value, rented.AsSpan(0, byteCount));
            WriteRaw(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public void WriteUtf8String(ReadOnlySpan<byte> utf8)
    {
        WriteStringHeader(utf8.Length);
        WriteRaw(utf8);
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        WriteBytesHeader(value.Length);
        WriteRaw(value);
    }

    public void WriteList(IReadOnlyList<object?> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        WriteListHeader(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            WriteObject(items[i]);
        }
    }

    public void WriteMap(IReadOnlyDictionary<string, object?> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        WriteMapHeader(map.Count);
        foreach (var kv in map)
        {
            WriteString(kv.Key);
            WriteObject(kv.Value);
        }
    }

    public void WriteStructHeader(byte tag, int fieldCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fieldCount);
        if (fieldCount < 0x10)
        {
            WriteByte((byte)(PackStreamMarker.TinyStruct | fieldCount));
            WriteByte(tag);
            return;
        }

        if (fieldCount <= byte.MaxValue)
        {
            WriteByte(PackStreamMarker.Struct8);
            WriteByte((byte)fieldCount);
            WriteByte(tag);
            return;
        }

        if (fieldCount <= short.MaxValue)
        {
            WriteByte(PackStreamMarker.Struct16);
            WriteBigEndian((short)fieldCount);
            WriteByte(tag);
            return;
        }

        throw new ProtocolException($"Structures cannot have more than {short.MaxValue} fields.");
    }

    public void WriteObject(object? value)
    {
        switch (value)
        {
            case null:
                WriteNull();
                break;
            case bool b:
                WriteBoolean(b);
                break;
            case byte u8:
                WriteInteger(u8);
                break;
            case sbyte s8:
                WriteInteger(s8);
                break;
            case short s16:
                WriteInteger(s16);
                break;
            case ushort u16:
                WriteInteger(u16);
                break;
            case int i32:
                WriteInteger(i32);
                break;
            case uint u32:
                WriteInteger(u32);
                break;
            case long i64:
                WriteInteger(i64);
                break;
            case ulong u64 when u64 <= long.MaxValue:
                WriteInteger((long)u64);
                break;
            case ulong:
                throw new NotSupportedException("Integral value is larger than long.MaxValue.");
            case float f:
                WriteFloat64(f);
                break;
            case double d:
                WriteFloat64(d);
                break;
            case string s:
                WriteString(s);
                break;
            case byte[] bytes:
                WriteBytes(bytes);
                break;
            case IReadOnlyDictionary<string, object?> ro:
                WriteMap(ro);
                break;
            case IReadOnlyList<object?> list:
                WriteList(list);
                break;
            case IList list:
                WriteListHeader(list.Count);
                foreach (var item in list)
                {
                    WriteObject(item);
                }

                break;
            default:
                throw new NotSupportedException($"PackStream encoding is not supported for type {value.GetType().FullName}.");
        }
    }

    private void WriteStringHeader(int utf8Length)
    {
        if (utf8Length < 0x10)
        {
            WriteByte((byte)(PackStreamMarker.TinyString | utf8Length));
        }
        else if (utf8Length <= byte.MaxValue)
        {
            WriteByte(PackStreamMarker.String8);
            WriteByte((byte)utf8Length);
        }
        else if (utf8Length <= short.MaxValue)
        {
            WriteByte(PackStreamMarker.String16);
            WriteBigEndian((short)utf8Length);
        }
        else
        {
            WriteByte(PackStreamMarker.String32);
            WriteBigEndian(utf8Length);
        }
    }

    private void WriteBytesHeader(int length)
    {
        if (length <= byte.MaxValue)
        {
            WriteByte(PackStreamMarker.Bytes8);
            WriteByte((byte)length);
        }
        else if (length <= short.MaxValue)
        {
            WriteByte(PackStreamMarker.Bytes16);
            WriteBigEndian((short)length);
        }
        else
        {
            WriteByte(PackStreamMarker.Bytes32);
            WriteBigEndian(length);
        }
    }

    private void WriteListHeader(int count)
    {
        if (count < 0x10)
        {
            WriteByte((byte)(PackStreamMarker.TinyList | count));
        }
        else if (count <= byte.MaxValue)
        {
            WriteByte(PackStreamMarker.List8);
            WriteByte((byte)count);
        }
        else if (count <= short.MaxValue)
        {
            WriteByte(PackStreamMarker.List16);
            WriteBigEndian((short)count);
        }
        else
        {
            WriteByte(PackStreamMarker.List32);
            WriteBigEndian(count);
        }
    }

    private void WriteMapHeader(int count)
    {
        if (count < 0x10)
        {
            WriteByte((byte)(PackStreamMarker.TinyMap | count));
        }
        else if (count <= byte.MaxValue)
        {
            WriteByte(PackStreamMarker.Map8);
            WriteByte((byte)count);
        }
        else if (count <= short.MaxValue)
        {
            WriteByte(PackStreamMarker.Map16);
            WriteBigEndian((short)count);
        }
        else
        {
            WriteByte(PackStreamMarker.Map32);
            WriteBigEndian(count);
        }
    }

    private void WriteRaw(ReadOnlySpan<byte> data)
    {
        var span = _writer.GetSpan(data.Length);
        data.CopyTo(span);
        _writer.Advance(data.Length);
    }

    private void WriteByte(byte value)
    {
        var span = _writer.GetSpan(1);
        span[0] = value;
        _writer.Advance(1);
    }

    private void WriteBigEndian(short value)
    {
        var span = _writer.GetSpan(2);
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        _writer.Advance(2);
    }

    private void WriteBigEndian(int value)
    {
        var span = _writer.GetSpan(4);
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        _writer.Advance(4);
    }

    private void WriteBigEndian(long value)
    {
        var span = _writer.GetSpan(8);
        BinaryPrimitives.WriteInt64BigEndian(span, value);
        _writer.Advance(8);
    }
}
