// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal ref struct SequencePackStreamReader
{
    private readonly MessageFormat _format;
    private Span<byte> _span;
    private SequenceReader<byte> _reader;
    private readonly CancellationToken _cancellationToken;

    public SequencePackStreamReader(MessageFormat format, ByteBuffers buffers, SequenceReader<byte> reader,
        CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _reader = reader;
        _format = format;
        _span = new Span<byte>(buffers.LongBuffer);
    }
    
    private object Read()
    {
        int length;
        _reader.TryRead(out var markerByte);
        var markerHighNibble = (byte)(markerByte & 0xF0);

        switch (markerHighNibble)
        {
            case PackStream.TinyString:
                length = markerByte & 0x0F;
                return ReadString(length);
            case PackStream.TinyList:
                length = markerByte & 0x0F;
                return ReadList(length);

            case PackStream.TinyMap:
                length = markerByte & 0x0F;
                return ReadMap(length);

            case PackStream.TinyStruct:
                length = markerByte & 0x0F;
                return ReadMap(length);
            default:
            {
                var tiny = (sbyte)markerByte;
                if (tiny >= PackStream.Minus2ToThe4)
                {
                    return tiny;
                }
                break;
            }
        }

        switch (markerByte)
        {
            case PackStream.Null:
                return null;
            case PackStream.True:
                return true;
            case PackStream.False:
                return false;
            case PackStream.Float64:
                return ReadDouble();
            case PackStream.Bytes8:
                length = ReadUint8AsInt32();
                return ReadBytes(length);
            case PackStream.Bytes16:
                length = ReadUint16AsInt32();
                return ReadBytes(length);
            case PackStream.Bytes32:
                length = ReadUint32AsInt32();
                return ReadBytes(length);
            case PackStream.String8:
                length = ReadUint8AsInt32();
                return ReadString(length);
            case PackStream.String16:
                length = ReadUint16AsInt32();
                return ReadString(length);
            case PackStream.String32:
                length = ReadUint32AsInt32();
                return ReadString(length);
            case PackStream.List8:
                length = ReadUint8AsInt32();
                return ReadList(length);
            case PackStream.List16:
                length = ReadUint16AsInt32();
                return ReadList(length);
            case PackStream.List32:
                length = ReadUint32AsInt32();
                return ReadList(length);
            case PackStream.Map8:
                length = ReadUint8AsInt32();
                return ReadMap(length);
            case PackStream.Map16:
                length = ReadUint16AsInt32();
                return ReadMap(length);
            case PackStream.Map32:
                length = ReadUint32AsInt32();
                return ReadMap(length);
            case PackStream.Struct8:
                length = ReadUint8AsInt32();
                var signature = NextByte();
                if (_format.ReaderStructHandlers.TryGetValue(signature, out var handler))
                {
                    return handler.DeserializeSequence(_format.Version, this, signature, length);
                }
                throw new ProtocolException($"Unknown type 0x{markerByte:X2}");
            case PackStream.Struct16:
                length = ReadUint16AsInt32();
                var struct16Signature = NextByte();
                if (_format.ReaderStructHandlers.TryGetValue(struct16Signature, out var handle))
                {
                    return handle.DeserializeSequence(_format.Version, this, struct16Signature, length);
                }
                throw new ProtocolException($"Unknown type 0x{markerByte:X2}");
            case PackStream.Int8:
                return (sbyte)NextByte();
            case PackStream.Int16:
                return NextShort();
            case PackStream.Int32:
                return NextInt();
            case PackStream.Int64:
                return NextLong();
            default:
                throw new ProtocolException($"Unknown type 0x{markerByte:X2}");
        }
    }

    private IList<object> ReadList(int length)
    {
        var list = new List<object>(length);
        for (var i = 0; i < length; i++)
        {
            list.Add(Read());
        }

        return list;
    }

    public IResponseMessage ReadMessage()
    {
        var size = ReadStructHeader();
        var signature = NextByte();

        if (_format.MessageReaders.TryGetValue(signature, out var handler))
        {
            return handler.DeserializeMessage(_format.Version, this, signature, size);
        }

        throw new ProtocolException("Unknown structure type: " + signature);
    }

    public Dictionary<string, object> ReadMap()
    {
        var size = ReadMapHeader();
        return ReadMap(size);
    }

    private Dictionary<string, object> ReadMap(int size)
    {
        if (size == 0)
        {
            return new Dictionary<string, object>(0);
        }

        var map = new Dictionary<string, object>(size);
        for (var i = 0; i < size; i++)
        {
            var key = ReadString();
            map.Add(key, Read());
        }

        return map;
    }

    public IList<object> ReadList()
    {
        var length = ReadListHeader();
        return ReadList(length);
    }

    public object ReadStruct()
    {
        var size = ReadStructHeader();
        var signature = NextByte();

        if (_format.ReaderStructHandlers.TryGetValue(signature, out var handler))
        {
            return handler.DeserializeSequence(_format.Version, this, signature, size);
        }

        throw new ProtocolException("Unknown structure type: " + signature);
    }

    public object ReadNull()
    {
        var marker = NextByte();
        if (marker == PackStream.Null)
        {
            return null;
        }

        throw new ProtocolException($"Expected a null, but got: 0x{marker & 0xFF:X2}");
    }

    public bool ReadBoolean()
    {
        var marker = NextByte();
        if (marker == PackStream.True)
        {
            return true;
        }

        if (marker == PackStream.False)
        {
            return false;
        }

        throw new ProtocolException($"Expected an boolean, but got: 0x{marker:X2}");
    }

    public int ReadInteger()
    {
        var marker = NextByte();
        if (marker is var _ && (sbyte)marker >= PackStream.Minus2ToThe4)
        {
            return (sbyte)marker;
        }

        if (marker == PackStream.Int8)
        {
            return (sbyte)NextByte();
        }

        if (marker == PackStream.Int16)
        {
            return NextShort();
        }

        if (marker == PackStream.Int32)
        {
            return NextInt();
        }

        if (marker == PackStream.Int64)
        {
            throw new OverflowException($"Unexpectedly large Integer value unpacked.");
        }

        throw new ProtocolException($"Expected an integer, but got: 0x{marker:X2}");
    }

    public long ReadLong()
    {
        var marker = NextByte();
        if (((sbyte)marker) >= PackStream.Minus2ToThe4)
        {
            return (sbyte)marker;
        }

        if (marker == PackStream.Int8)
        {
            return (sbyte)NextByte();
        }

        if (marker == PackStream.Int16)
        {
            return NextShort();
        }

        if (marker == PackStream.Int32)
        {
            return NextInt();
        }

        if (marker == PackStream.Int64)
        {
            return NextLong();
        }

        throw new ProtocolException($"Expected an integer, but got: 0x{marker:X2}");
    }

    public double ReadDouble()
    {
        var markerByte = NextByte();
        if (markerByte == PackStream.Float64)
        {
            return NextDouble();
        }

        throw new ProtocolException($"Expected a double, but got: 0x{markerByte:X2}");
    }

    public byte[] ReadBytes()
    {
        var markerByte = NextByte();

        int length;
        if (markerByte == PackStream.Bytes8)
        {
            length = ReadUint8AsInt32();
        }
        else if (markerByte == PackStream.Bytes16)
        {
            length = ReadUint16AsInt32();
        }
        else if (markerByte == PackStream.Bytes32)
        {
            length = ReadUint32AsInt32();
        }
        else
        {
            throw new ProtocolException($"Expected a string, but got: 0x{markerByte:X2}");
        }

        return ReadBytes(length);
    }

    private byte[] ReadBytes(int length)
    {
        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        var data = new byte[length];
        _reader.TryCopyTo(data.AsSpan());
        _reader.Advance(length);
        return data;
    }

    public string ReadString()
    {
        var markerByte = NextByte();
        // Note no mask, so we compare to 0x80.
        if (markerByte == PackStream.TinyString)
        {
            return string.Empty;
        }
        
        int length;
        if (markerByte == PackStream.String8)
        {
            length = ReadUint8AsInt32();
        }
        else if (markerByte == PackStream.String16)
        {
            length = ReadUint16AsInt32();
        }
        else if (markerByte == PackStream.String32)
        {
            length = ReadUint32AsInt32();
        }
        else if (markerByte is var _ && (markerByte & 0xF0) == PackStream.TinyString)
        {
            length = markerByte & 0x0F;
        }
        else
        {
            throw new ProtocolException($"Expected a string, but got: 0x{markerByte:X2}");
        }

        return ReadString(length);
    }

    private string ReadString(int length)
    {
        if (length < 8)
        {
            var span = _span.Slice(0, length);
            _reader.TryCopyTo(span);
            _reader.Advance(length);
            return Encoding.UTF8.GetString(span);
        }

        using var memory = MemoryPool<byte>.Shared.Rent(length);
        var buffer = memory.Memory.Span.Slice(0, length);
        _reader.TryCopyTo(buffer);
        _reader.Advance(length);
        return Encoding.UTF8.GetString(buffer);
    }

    public int ReadMapHeader()
    {
        var marker = NextByte();
        if ((marker & 0xF0) == PackStream.TinyMap)
        {
            return marker & 0x0F;
        }

        if (marker == PackStream.Map8)
        {
            return ReadUint8AsInt32();
        }

        if (marker == PackStream.Map16)
        {
            return ReadUint16AsInt32();
        }

        if (marker == PackStream.Map32)
        {
            return ReadUint32AsInt32();
        }

        throw new ProtocolException($"Expected a map, but got: 0x{marker:X2}");
    }

    public int ReadListHeader()
    {
        var marker = NextByte();
        if ((marker & 0xF0) == PackStream.TinyList)
        {
            return marker & 0x0F;
        }

        if (marker == PackStream.List8)
        {
            return ReadUint8AsInt32();
        }

        if (marker == PackStream.List16)
        {
            return ReadUint16AsInt32();
        }

        if (marker == PackStream.List32)
        {
            return ReadUint32AsInt32();
        }

        throw new ProtocolException($"Expected a list, but got: 0x{marker:X2}");
    }

    public int ReadStructHeader()
    {
        var marker = NextByte();
        if ((marker & 0xF0) == PackStream.TinyStruct)
        {
            return marker & 0x0F;
        }

        if (marker == PackStream.Struct8)
        {
            return ReadUint8AsInt32();
        }

        if (marker == PackStream.Struct16)
        {
            return ReadUint16AsInt32();
        }

        throw new ProtocolException($"Expected a struct, but got: 0x{marker:X2}");
    }

    private int ReadUint8AsInt32()
    {
        return NextByte() & 0xFF;
    }

    private int ReadUint16AsInt32()
    {
        return NextShort() & 0xFFFF;
    }

    private int ReadUint32AsInt32()
    {
        return (int)(NextInt() & 0xFFFFFFFFL);
    }

    public byte NextByte()
    {
        _reader.TryRead(out var b);
        return b;
    }

    public short NextShort()
    {
        var slice = _span.Slice(0, 2);
        _reader.TryCopyTo(slice);
        _reader.Advance(2);
        return BinaryPrimitives.ReadInt16BigEndian(slice);
    }

    public int NextInt()
    {
        var slice = _span.Slice(0, 4);
        _reader.TryCopyTo(slice);
        _reader.Advance(4);
        return BinaryPrimitives.ReadInt32BigEndian(slice);
    }

    public long NextLong()
    {
        _reader.TryCopyTo(_span);
        _reader.Advance(8);
        return BinaryPrimitives.ReadInt64BigEndian(_span);
    }

    public double NextDouble()
    {
        _reader.TryCopyTo(_span);
        _reader.Advance(8);
        return BinaryPrimitives.ReadDoubleBigEndian(_span);
    }
}
