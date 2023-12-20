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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO;

// we don't use the return value of Read() in most cases, so suppress the warning
[SuppressMessage("ReSharper", "MustUseReturnValue")]

internal sealed class PackStreamReader
{
    public ByteBuffers Buffers { get; }
    public MessageFormat Format { get; }

    public MemoryStream Stream;

    internal PackStreamReader(MessageFormat format, MemoryStream stream, ByteBuffers buffers)
    {
        Format = format;
        Stream = stream;
        Buffers = buffers;
    }

    public object Read()
    {
        var type = PeekNextType();
        var result = ReadValue(type);
        return result;
    }

    public IResponseMessage ReadMessage()
    {
        var size = ReadStructHeader();
        var signature = ReadStructSignature();

        if (Format.ReaderStructHandlers.TryGetValue(signature, out var handler))
        {
            return (IResponseMessage)handler.Deserialize(Format.Version, this, signature, size);
        }

        throw new ProtocolException("Unknown structure type: " + signature);
    }

    public Dictionary<string, object> ReadMap()
    {
        var size = (int)ReadMapHeader();
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
        var size = (int)ReadListHeader();
        var vals = new object[size];
        for (var j = 0; j < size; j++)
        {
            vals[j] = Read();
        }

        return new List<object>(vals);
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

    public object ReadStruct()
    {
        var size = ReadStructHeader();
        var signature = ReadStructSignature();

        if (Format.ReaderStructHandlers.TryGetValue(signature, out var handler))
        {
            return handler.Deserialize(Format.Version, this, signature, size);
        }

        throw new ProtocolException("Unknown structure type: " + signature);
    }

    public object ReadNull()
    {
        var markerByte = NextByte();
        if (markerByte != PackStream.Null)
        {
            throw new ProtocolException($"Expected a null, but got: 0x{markerByte & 0xFF:X2}");
        }

        return null;
    }

    public bool ReadBoolean()
    {
        var markerByte = NextByte();
        switch (markerByte)
        {
            case PackStream.True:
                return true;

            case PackStream.False:
                return false;

            default:
                throw new ProtocolException($"Expected a boolean, but got: 0x{markerByte & 0xFF:X2}");
        }
    }

    public int ReadInteger()
    {
        var markerByte = NextByte();
        if ((sbyte)markerByte >= PackStream.Minus2ToThe4)
        {
            return (sbyte)markerByte;
        }

        switch (markerByte)
        {
            case PackStream.Int8:
                return NextSByte();

            case PackStream.Int16:
                return NextShort();

            case PackStream.Int32:
                return NextInt();

            case PackStream.Int64:
                throw new OverflowException($"Unexpectedly large Integer value unpacked {NextLong()}");

            default:
                throw new ProtocolException($"Expected an integer, but got: 0x{markerByte:X2}");
        }
    }

    public long ReadLong()
    {
        var markerByte = NextByte();
        if ((sbyte)markerByte >= PackStream.Minus2ToThe4)
        {
            return (sbyte)markerByte;
        }

        switch (markerByte)
        {
            case PackStream.Int8:
                return NextSByte();

            case PackStream.Int16:
                return NextShort();

            case PackStream.Int32:
                return NextInt();

            case PackStream.Int64:
                return NextLong();

            default:
                throw new ProtocolException($"Expected an integer, but got: 0x{markerByte:X2}");
        }
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

    public string ReadString()
    {
        var markerByte = NextByte();
        if (markerByte == PackStream.TinyString) // Note no mask, so we compare to 0x80.
        {
            return string.Empty;
        }

        return PackStreamBitConverter.ToString(ReadUtf8(markerByte));
    }

    public byte[] ReadBytes()
    {
        var markerByte = NextByte();

        switch (markerByte)
        {
            case PackStream.Bytes8:
                return ReadBytes(ReadUint8());

            case PackStream.Bytes16:
                return ReadBytes(ReadUint16());

            case PackStream.Bytes32:
            {
                var size = ReadUint32();
                if (size <= int.MaxValue)
                {
                    return ReadBytes((int)size);
                }

                throw new ProtocolException($"BYTES_32 {size} too long for PackStream");
            }

            default:
                throw new ProtocolException($"Expected binary data, but got: 0x{markerByte & 0xFF:X2}");
        }
    }

    internal byte[] ReadBytes(int size)
    {
        if (size == 0)
        {
            return Array.Empty<byte>();
        }

        var heapBuffer = new byte[size];
        Stream.Read(heapBuffer);
        return heapBuffer;
    }

    private byte[] ReadUtf8(byte markerByte)
    {
        var markerHighNibble = (byte)(markerByte & 0xF0);
        var markerLowNibble = (byte)(markerByte & 0x0F);

        if (markerHighNibble == PackStream.TinyString)
        {
            return ReadBytes(markerLowNibble);
        }

        switch (markerByte)
        {
            case PackStream.String8:
                return ReadBytes(ReadUint8());

            case PackStream.String16:
                return ReadBytes(ReadUint16());

            case PackStream.String32:
            {
                var size = ReadUint32();
                if (size <= int.MaxValue)
                {
                    return ReadBytes((int)size);
                }

                throw new ProtocolException($"STRING_32 {size} too long for PackStream");
            }

            default:
                throw new ProtocolException($"Expected a string, but got: 0x{markerByte & 0xFF:X2}");
        }
    }

    public long ReadMapHeader()
    {
        var markerByte = Stream.ReadByte();
        var markerHighNibble = (byte)(markerByte & 0xF0);
        var markerLowNibble = (byte)(markerByte & 0x0F);

        if (markerHighNibble == PackStream.TinyMap)
        {
            return markerLowNibble;
        }

        switch (markerByte)
        {
            case PackStream.Map8:
                return ReadUint8();

            case PackStream.Map16:
                return ReadUint16();

            case PackStream.Map32:
                return ReadUint32();

            default:
                throw new ProtocolException($"Expected a map, but got: 0x{markerByte:X2}");
        }
    }

    public long ReadListHeader()
    {
        var markerByte = Stream.ReadByte();
        var markerHighNibble = (byte)(markerByte & 0xF0);
        var markerLowNibble = (byte)(markerByte & 0x0F);

        if (markerHighNibble == PackStream.TinyList)
        {
            return markerLowNibble;
        }

        switch (markerByte)
        {
            case PackStream.List8:
                return ReadUint8();

            case PackStream.List16:
                return ReadUint16();

            case PackStream.List32:
                return ReadUint32();

            default:
                throw new ProtocolException($"Expected a list, but got: 0x{markerByte & 0xFF:X2}");
        }
    }

    public byte ReadStructSignature()
    {
        return NextByte();
    }

    public long ReadStructHeader()
    {
        var markerByte = Stream.ReadByte();
        var markerHighNibble = (byte)(markerByte & 0xF0);
        var markerLowNibble = (byte)(markerByte & 0x0F);

        if (markerHighNibble == PackStream.TinyStruct)
        {
            return markerLowNibble;
        }

        switch (markerByte)
        {
            case PackStream.Struct8:
                return ReadUint8();

            case PackStream.Struct16:
                return ReadUint16();

            default:
                throw new ProtocolException($"Expected a struct, but got: 0x{markerByte:X2}");
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

    private int ReadUint8()
    {
        return NextByte() & 0xFF;
    }

    private int ReadUint16()
    {
        return NextShort() & 0xFFFF;
    }

    private long ReadUint32()
    {
        return NextInt() & 0xFFFFFFFFL;
    }

    internal sbyte NextSByte()
    {
        Stream.Read(Buffers.ByteArray);
        return (sbyte)Buffers.ByteArray[0];
    }

    public byte NextByte()
    {
        Stream.Read(Buffers.ByteArray);

        return Buffers.ByteArray[0];
    }

    public short NextShort()
    {
        Stream.Read(Buffers.ShortBuffer);

        return PackStreamBitConverter.ToInt16(Buffers.ShortBuffer);
    }

    public int NextInt()
    {
        Stream.Read(Buffers.IntBuffer);

        return PackStreamBitConverter.ToInt32(Buffers.IntBuffer);
    }

    public long NextLong()
    {
        Stream.Read(Buffers.LongBuffer);

        return PackStreamBitConverter.ToInt64(Buffers.LongBuffer);
    }

    public double NextDouble()
    {
        Stream.Read(Buffers.LongBuffer);

        return PackStreamBitConverter.ToDouble(Buffers.LongBuffer);
    }

    public byte PeekByte()
    {
        if (Stream.Length - Stream.Position < 1)
        {
            throw new ProtocolException("Unable to peek 1 byte from buffer.");
        }

        try
        {
            return (byte)Stream.ReadByte();
        }
        finally
        {
            Stream.Seek(-1, SeekOrigin.Current);
        }
    }
}
