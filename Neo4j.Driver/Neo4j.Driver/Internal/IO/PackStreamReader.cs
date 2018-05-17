// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.IO;
using System.Text;
using Neo4j.Driver.V1;
using Neo4j.Driver.Internal;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class PackStreamReader: IPackStreamReader
    {
        private static readonly Dictionary<string, object> EmptyStringValueMap = new Dictionary<string, object>();
        private static readonly byte[] EmptyByteArray = new byte[0];

        private readonly IDictionary<byte, IPackStreamStructHandler> _structHandlers;

        private readonly byte[] _byteBuffer = new byte[1];
        private readonly byte[] _shortBuffer = new byte[2];
        private readonly byte[] _intBuffer = new byte[4];
        private readonly byte[] _longBuffer = new byte[8];

        private readonly Stream _stream;

        public PackStreamReader(Stream stream, IDictionary<byte, IPackStreamStructHandler> structHandlers)
        {
            Throw.ArgumentNullException.IfNull(stream, nameof(stream));
            Throw.ArgumentOutOfRangeException.IfFalse(stream.CanRead, nameof(stream));

            _stream = stream;
            _structHandlers = structHandlers ?? new Dictionary<byte, IPackStreamStructHandler>();
        }

        public object Read()
        {
            var type = PeekNextType();
            var result = ReadValue(type);
            return result;
        }

        public Dictionary<string, object> ReadMap()
        {
            var size = (int)ReadMapHeader();
            if (size == 0)
            {
                return EmptyStringValueMap;
            }
            var map = new Dictionary<string, object>(size);
            for (var i = 0; i < size; i++)
            {
                var key = ReadString();
                map.Add(key, Read());
            }
            return map;
        }

        private IList<object> ReadList()
        {
            var size = (int)ReadListHeader();
            var vals = new object[size];
            for (var j = 0; j < size; j++)
            {
                vals[j] = Read();
            }
            return new List<object>(vals);
        }

        protected internal virtual object ReadValue(PackType type)
        {
            switch (type)
            {
                case PackType.Bytes:
                    return ReadBytes();
                case PackType.Null:
                    return ReadNull();
                case PackType.Boolean:
                    return ReadBoolean();
                case PackType.Integer:
                    return ReadLong();
                case PackType.Float:
                    return ReadDouble();
                case PackType.String:
                    return ReadString();
                case PackType.Map:
                    return ReadMap();
                case PackType.List:
                    return ReadList();
                case PackType.Struct:
                    return ReadStruct();
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown value type: {type}");
        }

        public object ReadStruct()
        {
            var size = ReadStructHeader();
            var signature = ReadStructSignature();

            if (_structHandlers.TryGetValue(signature, out var handler))
            {
                return handler.Read(this, size);
            }

            throw new ProtocolException("Unknown structure type: " + signature);
        }
        
        public object ReadNull()
        {
            byte markerByte = NextByte();
            if (markerByte != Null)
            {
                throw new ProtocolException(
                    $"Expected a null, but got: 0x{(markerByte & 0xFF):X2}");
            }
            return null;
        }

        public bool ReadBoolean()
        {
            byte markerByte = NextByte();
            switch (markerByte)
            {
                case True:
                    return true;
                case False:
                    return false;
                default:
                    throw new ProtocolException(
                        $"Expected a boolean, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public long ReadLong()
        {
            byte markerByte = NextByte();
            if ((sbyte)markerByte >= Minus2ToThe4)
            {
                return (sbyte)markerByte;
            }
            switch (markerByte)
            {
                case Int8:
                    return NextSByte();
                case PackStream.Int16:
                    return NextShort();
                case PackStream.Int32:
                    return NextInt();
                case PackStream.Int64:
                    return NextLong();
                default:
                    throw new ProtocolException(
                        $"Expected an integer, but got: 0x{markerByte:X2}");
            }
        }

        public double ReadDouble()
        {
            byte markerByte = NextByte();
            if (markerByte == Float64)
            {
                return NextDouble();
            }
            throw new ProtocolException(
                $"Expected a double, but got: 0x{markerByte:X2}");
        }

        public string ReadString()
        {
            var markerByte = NextByte();
            if (markerByte == TinyString) // Note no mask, so we compare to 0x80.
            {
                return string.Empty;
            }

            return PackStreamBitConverter.ToString(ReadUtf8(markerByte));
        }

        public virtual byte[] ReadBytes()
        {
            byte markerByte = NextByte();

            switch (markerByte)
            {
                case Bytes8:
                    return ReadBytes(ReadUint8());
                case Bytes16:
                    return ReadBytes(ReadUint16());
                case Bytes32:
                {
                    long size = ReadUint32();
                    if (size <= int.MaxValue)
                    {
                        return ReadBytes((int)size);
                    }
                    else
                    {
                        throw new ProtocolException(
                            $"BYTES_32 {size} too long for PackStream");
                    }
                }
                default:
                    throw new ProtocolException(
                        $"Expected binary data, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        internal byte[] ReadBytes(int size)
        {
            if (size == 0)
            {
                return EmptyByteArray;
            }

            var heapBuffer = new byte[size];
            _stream.Read(heapBuffer);
            return heapBuffer;
        }

        private byte[] ReadUtf8(byte markerByte)
        {
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

            if (markerHighNibble == TinyString)
            {
                return ReadBytes(markerLowNibble);
            }
            switch (markerByte)
            {
                case String8:
                    return ReadBytes(ReadUint8());
                case String16:
                    return ReadBytes(ReadUint16());
                case String32:
                {
                    var size = ReadUint32();
                    if (size <= int.MaxValue)
                    {
                        return ReadBytes((int)size);
                    }
                    throw new ProtocolException(
                        $"STRING_32 {size} too long for PackStream");
                }
                default:
                    throw new ProtocolException(
                        $"Expected a string, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public long ReadMapHeader()
        {
            var markerByte = _stream.ReadByte();
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

            if (markerHighNibble == TinyMap)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case Map8:
                    return ReadUint8();
                case Map16:
                    return ReadUint16();
                case Map32:
                    return ReadUint32();
                default:
                    throw new ProtocolException(
                        $"Expected a map, but got: 0x{markerByte:X2}");
            }
        }

        public long ReadListHeader()
        {
            var markerByte = _stream.ReadByte();
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

            if (markerHighNibble == TinyList)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case List8:
                    return ReadUint8();
                case List16:
                    return ReadUint16();
                case List32:
                    return ReadUint32();
                default:
                    throw new ProtocolException(
                        $"Expected a list, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public byte ReadStructSignature()
        {
            return NextByte();
        }

        public long ReadStructHeader()
        {
            var markerByte = _stream.ReadByte();
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

            if (markerHighNibble == TinyStruct)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case Struct8:
                    return ReadUint8();
                case Struct16:
                    return ReadUint16();
                default:
                    throw new ProtocolException(
                        $"Expected a struct, but got: 0x{markerByte:X2}");
            }
        }

        public PackType PeekNextType()
        {
            var markerByte = PeekByte();
            var markerHighNibble = (byte)(markerByte & 0xF0);

            switch (markerHighNibble)
            {
                case TinyString:
                    return PackType.String;
                case TinyList:
                    return PackType.List;
                case TinyMap:
                    return PackType.Map;
                case TinyStruct:
                    return PackType.Struct;
            }

            if ((sbyte)markerByte >= Minus2ToThe4)
                return PackType.Integer;

            switch (markerByte)
            {
                case Null:
                    return PackType.Null;
                case True:
                case False:
                    return PackType.Boolean;
                case Float64:
                    return PackType.Float;
                case Bytes8:
                case Bytes16:
                case Bytes32:
                    return PackType.Bytes;
                case String8:
                case String16:
                case String32:
                    return PackType.String;
                case List8:
                case List16:
                case List32:
                    return PackType.List;
                case Map8:
                case Map16:
                case Map32:
                    return PackType.Map;
                case Struct8:
                case Struct16:
                    return PackType.Struct;
                case Int8:
                case PackStream.Int16:
                case PackStream.Int32:
                case PackStream.Int64:
                    return PackType.Integer;
                default:
                    throw new ProtocolException(
                        $"Unknown type 0x{markerByte:X2}");
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
            _stream.Read(_byteBuffer);

            return (sbyte)_byteBuffer[0];
        }

        public byte NextByte()
        {
            _stream.Read(_byteBuffer);

            return (byte)_byteBuffer[0];
        }

        public short NextShort()
        {
            _stream.Read(_shortBuffer);

            return PackStreamBitConverter.ToInt16(_shortBuffer);
        }

        public int NextInt()
        {
            _stream.Read(_intBuffer);

            return PackStreamBitConverter.ToInt32(_intBuffer);
        }

        public long NextLong()
        {
            _stream.Read(_longBuffer);

            return PackStreamBitConverter.ToInt64(_longBuffer);
        }

        public double NextDouble()
        {
            _stream.Read(_longBuffer);

            return PackStreamBitConverter.ToDouble(_longBuffer);
        }

        public byte PeekByte()
        {
            if (_stream.Length - _stream.Position < 1)
            {
                throw new ProtocolException("Unable to peek 1 byte from buffer.");
            }

            try
            {
                return (byte)_stream.ReadByte();
            }
            finally
            {
                _stream.Seek(-1, SeekOrigin.Current);
            }
        }

    }
}
