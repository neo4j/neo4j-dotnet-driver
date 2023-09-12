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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

internal ref struct SpanPackStreamReader
{
    private readonly MessageFormat _format;
    private readonly ReadOnlySpan<byte> _reader;
    public int Index = 0;
    private readonly CancellationToken _cancellationToken;

    public SpanPackStreamReader(
        MessageFormat format,
        ReadOnlySpan<byte> reader,
        CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        _reader = reader;
        _format = format;
    }

    public object Read()
    {
        var type = PeekNextType();
        var result = ReadValue(type);
        return result;
    }

    private object ReadValue(PackStreamType streamType)
    {
        switch (streamType)
        {
            case PackStreamType.Bytes:
                return ReadBytes();

            case PackStreamType.Null:
                return ReadNull();

            case PackStreamType.Boolean:
                return ReadBoolean();

            case PackStreamType.Integer:
                return ReadLong();

            case PackStreamType.Float:
                return ReadDouble();

            case PackStreamType.String:
                return ReadString();

            case PackStreamType.Map:
                return ReadMap();

            case PackStreamType.List:
                return ReadList();

            case PackStreamType.Struct:
                return ReadStruct();

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(streamType),
                    streamType,
                    $"Unknown value type: {streamType}");
        }
    }

    internal PackStreamType PeekNextType()
    {
        var markerByte = PeekByte();
        var markerHighNibble = (byte)(markerByte & 0xF0);

        switch (markerHighNibble)
        {
            case PackStream.TinyString:
                return PackStreamType.String;

            case PackStream.TinyList:
                return PackStreamType.List;

            case PackStream.TinyMap:
                return PackStreamType.Map;

            case PackStream.TinyStruct:
                return PackStreamType.Struct;
        }

        if ((sbyte)markerByte >= PackStream.Minus2ToThe4)
        {
            return PackStreamType.Integer;
        }

        switch (markerByte)
        {
            case PackStream.Null:
                return PackStreamType.Null;

            case PackStream.True:
            case PackStream.False:
                return PackStreamType.Boolean;

            case PackStream.Float64:
                return PackStreamType.Float;

            case PackStream.Bytes8:
            case PackStream.Bytes16:
            case PackStream.Bytes32:
                return PackStreamType.Bytes;

            case PackStream.String8:
            case PackStream.String16:
            case PackStream.String32:
                return PackStreamType.String;

            case PackStream.List8:
            case PackStream.List16:
            case PackStream.List32:
                return PackStreamType.List;

            case PackStream.Map8:
            case PackStream.Map16:
            case PackStream.Map32:
                return PackStreamType.Map;

            case PackStream.Struct8:
            case PackStream.Struct16:
                return PackStreamType.Struct;

            case PackStream.Int8:
            case PackStream.Int16:
            case PackStream.Int32:
            case PackStream.Int64:
                return PackStreamType.Integer;

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
        ReadStructHeader();
        var signature = NextByte();

        if (_format.MessageReaders.TryGetValue(signature, out var handler))
        {
            return handler.DeserializeMessage(_format.Version, this);
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
            var (o, idx) = handler.DeserializeSpan(_format.Version, this, signature, size);
            Index = idx;
            return o;
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
        if ((sbyte)marker >= PackStream.Minus2ToThe4)
        {
            return (sbyte)marker;
        }

        return marker switch
        {
            PackStream.Int8 => NextSByte(),
            PackStream.Int16 => NextShort(),
            PackStream.Int32 => NextInt(),
            PackStream.Int64 => throw new OverflowException($"Unexpectedly large Integer value unpacked."),
            _ => throw new ProtocolException($"Expected an integer, but got: 0x{marker:X2}")
        };
    }

    public long ReadLong()
    {
        var marker = NextByte();
        if ((sbyte)marker >= PackStream.Minus2ToThe4)
        {
            return (sbyte)marker;
        }

        return marker switch
        {
            PackStream.Int8 => NextSByte(),
            PackStream.Int16 => NextShort(),
            PackStream.Int32 => NextInt(),
            PackStream.Int64 => NextLong(),
            _ => throw new ProtocolException($"Expected an integer, but got: 0x{marker:X2}")
        };
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

        var slice = _reader.Slice(Index, length);
        var data = new byte[length];
        slice.CopyTo(new Span<byte>(data));
        Index += length;
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
        else if ((markerByte & 0xF0) == PackStream.TinyString)
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
        var slice = _reader.Slice(Index, length);
        Index += length;
#if NET6_0_OR_GREATER
        return Encoding.UTF8.GetString(slice);
#else
        return Encoding.UTF8.GetString(slice.ToArray());
#endif
    }

    public int ReadMapHeader()
    {
        var marker = NextByte();
        if ((byte)(marker & 0xF0) == PackStream.TinyMap)
        {
            return marker & 0x0F;
        }

        return marker switch
        {
            PackStream.Map8 => ReadUint8AsInt32(),
            PackStream.Map16 => ReadUint16AsInt32(),
            PackStream.Map32 => ReadUint32AsInt32(),
            _ => throw new ProtocolException($"Expected a map, but got: 0x{marker:X2}")
        };
    }

    public int ReadListHeader()
    {
        var marker = NextByte();
        if ((byte)(marker & 0xF0) == PackStream.TinyList)
        {
            return marker & 0x0F;
        }

        return marker switch
        {
            PackStream.List8 => ReadUint8AsInt32(),
            PackStream.List16 => ReadUint16AsInt32(),
            PackStream.List32 => ReadUint32AsInt32(),
            _ => throw new ProtocolException($"Expected a list, but got: 0x{marker:X2}")
        };
    }

    public int ReadStructHeader()
    {
        var marker = NextByte();
        if ((byte)(marker & 0xF0) == PackStream.TinyStruct)
        {
            return marker & 0x0F;
        }

        return marker switch
        {
            PackStream.Struct8 => ReadUint8AsInt32(),
            PackStream.Struct16 => ReadUint16AsInt32(),
            _ => throw new ProtocolException($"Expected a struct, but got: 0x{marker:X2}")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadUint8AsInt32()
    {
        return NextByte() & 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadUint16AsInt32()
    {
        return NextShort() & 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadUint32AsInt32()
    {
        return (int)(NextInt() & 0xFFFFFFFFL);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte()
    {
        return _reader[Index++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal sbyte NextSByte()
    {
        return (sbyte)_reader[Index++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short NextShort()
    {
        var slice = _reader.Slice(Index, 2);
        Index += 2;
        return BinaryPrimitives.ReadInt16BigEndian(slice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextInt()
    {
        var slice = _reader.Slice(Index, 4);
        Index += 4;
        return BinaryPrimitives.ReadInt32BigEndian(slice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long NextLong()
    {
        var slice = _reader.Slice(Index, 8);
        Index += 8;
        return BinaryPrimitives.ReadInt64BigEndian(slice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble()
    {
        var slice = _reader.Slice(Index, 8);
        Index += 8;
        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(slice));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte PeekByte()
    {
        return _reader[Index];
    }
}
