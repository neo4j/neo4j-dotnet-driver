// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Collections;
using System.Globalization;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Packstream
{
    internal class PackStream
    {
        public enum PackType
        {
            Null,
            Boolean,
            Integer,
            Float,
            Bytes,
            String,
            List,
            Map,
            Struct
        }

        #region Consts
        public const byte TINY_STRING = 0x80;
        public const byte TINY_LIST = 0x90;
        public const byte TINY_MAP = 0xA0;
        public const byte TINY_STRUCT = 0xB0;
        public const byte NULL = 0xC0;
        public const byte FLOAT_64 = 0xC1;
        public const byte FALSE = 0xC2;
        public const byte TRUE = 0xC3;
        public const byte RESERVED_C4 = 0xC4;
        public const byte RESERVED_C5 = 0xC5;
        public const byte RESERVED_C6 = 0xC6;
        public const byte RESERVED_C7 = 0xC7;
        public const byte INT_8 = 0xC8;
        public const byte INT_16 = 0xC9;
        public const byte INT_32 = 0xCA;
        public const byte INT_64 = 0xCB;
        public const byte BYTES_8 = 0xCC;
        public const byte BYTES_16 = 0xCD;
        public const byte BYTES_32 = 0xCE;
        public const byte RESERVED_CF = 0xCF;
        public const byte STRING_8 = 0xD0;
        public const byte STRING_16 = 0xD1;
        public const byte STRING_32 = 0xD2;
        public const byte RESERVED_D3 = 0xD3;
        public const byte LIST_8 = 0xD4;
        public const byte LIST_16 = 0xD5;
        public const byte LIST_32 = 0xD6;
        public const byte RESERVED_D7 = 0xD7;
        public const byte MAP_8 = 0xD8;
        public const byte MAP_16 = 0xD9;
        public const byte MAP_32 = 0xDA;
        public const byte RESERVED_DB = 0xDB;
        public const byte STRUCT_8 = 0xDC;
        public const byte STRUCT_16 = 0xDD;
        public const byte RESERVED_DE = 0xDE;
        public const byte RESERVED_DF = 0xDF;
        public const byte RESERVED_E0 = 0xE0;
        public const byte RESERVED_E1 = 0xE1;
        public const byte RESERVED_E2 = 0xE2;
        public const byte RESERVED_E3 = 0xE3;
        public const byte RESERVED_E4 = 0xE4;
        public const byte RESERVED_E5 = 0xE5;
        public const byte RESERVED_E6 = 0xE6;
        public const byte RESERVED_E7 = 0xE7;
        public const byte RESERVED_E8 = 0xE8;
        public const byte RESERVED_E9 = 0xE9;
        public const byte RESERVED_EA = 0xEA;
        public const byte RESERVED_EB = 0xEB;
        public const byte RESERVED_EC = 0xEC;
        public const byte RESERVED_ED = 0xED;
        public const byte RESERVED_EE = 0xEE;
        public const byte RESERVED_EF = 0xEF;

        public const long PLUS_2_TO_THE_31 = 2147483648L;
        public const long PLUS_2_TO_THE_15 = 32768L;
        public const long PLUS_2_TO_THE_7 = 128L;
        public const long MINUS_2_TO_THE_4 = -16L;
        public const long MINUS_2_TO_THE_7 = -128L;
        public const long MINUS_2_TO_THE_15 = -32768L;
        public const long MINUS_2_TO_THE_31 = -2147483648L;
        #endregion Consts

        public class Packer
        {

            private readonly IOutputStream _out;
            private static readonly BitConverterBase BitConverter = SocketClient.BitConverter;

            public Packer(IOutputStream outputStream)
            {
                _out = outputStream;
            }

            public void PackNull()
            {
                _out.Write(NULL);
            }

            private void PackRaw(byte[] data)
            {
                _out.Write(data);
            }

            public void Pack(long value)
            {
                if (value >= MINUS_2_TO_THE_4 && value < PLUS_2_TO_THE_7)
                {
                    _out.Write((byte) value);
                }
                else if (value >= MINUS_2_TO_THE_7 && value < MINUS_2_TO_THE_4)
                {
                    _out.Write(INT_8).Write(BitConverter.GetBytes((byte)value));// (byte) value;
                }
                else if (value >= MINUS_2_TO_THE_15 && value < PLUS_2_TO_THE_15)
                {
                    _out.Write(INT_16).Write(BitConverter.GetBytes((short) value));
                }
                else if (value >= MINUS_2_TO_THE_31 && value < PLUS_2_TO_THE_31)
                {
                    _out.Write(INT_32).Write(BitConverter.GetBytes((int) value));
                }
                else
                {
                    _out.Write(INT_64).Write(BitConverter.GetBytes(value));
                }
            }

            public void Pack(double value)
            {
                _out.Write(FLOAT_64).Write(BitConverter.GetBytes(value));
            }

            public void Pack(bool value)
            {
                _out.Write(value ? TRUE : FALSE);
            }

            public void Pack(string value)
            {
                if (value == null)
                {
                    PackNull();
                    return;
                }

                var bytes = BitConverter.GetBytes(value);
                PackStringHeader(bytes.Length);
                _out.Write(bytes);
            }

            public void Pack(byte[] values)
            {
                if (values == null)
                {
                    PackNull();
                }
                else
                {
                    PackBytesHeader(values.Length);
                    PackRaw(values);
                }
            }

            public void Pack(object value)
            {
                if (value == null)
                {
                    PackNull();
                }
                else if (value is bool)
                {
                    Pack((bool) value);
                }

                else if (value is sbyte || value is byte || value is short || value is int || value is long)
                {
                    Pack(Convert.ToInt64(value));
                }
                else if (value is byte[])
                {
                    Pack((byte[]) value);
                }
                else if (value is float || value is double || value is decimal)
                {
                    Pack(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (value is char || value is string)
                {
                    Pack(value.ToString());
                }
                else if (value is IList)
                {
                    Pack((IList) value);
                }
                else if (value is IDictionary)
                {
                    Pack((IDictionary) value);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value.GetType(),
                        $"Cannot understand{nameof(value)} with type {value.GetType().FullName}");
                }
            }

            public void Pack(IList value)
            {
                if (value == null)
                {
                    PackNull();
                    return;
                }
                PackListHeader(value.Count);
                foreach (var item in value)
                {
                    Pack(item);
                }
            }

            public void Pack(IDictionary values)
            {
                if (values == null)
                {
                    PackNull();
                }
                else
                {
                    PackMapHeader(values.Count);
                    foreach (var key in values.Keys)
                    {
                        Pack(key);
                        Pack(values[key]);
                    }
                }
            }

            public void PackBytesHeader(int size)
            {
                if (size <= byte.MaxValue)
                {
                    _out.Write(BYTES_8,(byte)size);
                }
                else if (size <= short.MaxValue)
                {
                    _out.Write(BYTES_16,BitConverter.GetBytes((short) size));
                }
                else
                {
                    _out.Write(BYTES_32, BitConverter.GetBytes(size));
                }
            }

            public void PackListHeader(int size)
            {
                if (size < 0x10)
                {
                    _out.Write((byte) (TINY_LIST | size));
                }
                else if (size <= byte.MaxValue)
                {
                    _out.Write(LIST_8, (byte) size);
                }
                else if (size <= short.MaxValue)
                {
                    _out.Write(LIST_16, BitConverter.GetBytes((short) size));
                }
                else
                {
                    _out.Write(LIST_32, BitConverter.GetBytes(size));
                }
            }

            public void PackMapHeader(int size)
            {
                if (size < 0x10)
                {
                    _out.Write((byte) (TINY_MAP | size));
                }
                else if (size <= byte.MaxValue)
                {
                    _out.Write(MAP_8, (byte) size);
                }
                else if (size <= short.MaxValue)
                {
                    _out.Write(MAP_16, BitConverter.GetBytes((short) size));
                }
                else
                {
                    _out.Write(MAP_32, BitConverter.GetBytes(size));
                }
            }

            public void PackStringHeader(int size)
            {
                if (size < 0x10)
                {
                    _out.Write((byte) (TINY_STRING | size));
                }
                else if (size <= byte.MaxValue)
                {
                    _out.Write(STRING_8, (byte) size);
                }
                else if (size <= short.MaxValue)
                {
                    _out.Write(STRING_16, BitConverter.GetBytes((short) size));
                }
                else
                {
                    _out.Write(STRING_32, BitConverter.GetBytes(size));
                }
            }

            public void PackStructHeader(int size, byte signature)
            {
                if (size < 0x10)
                {
                    _out.Write((byte) (TINY_STRUCT | size), signature);
                }
                else if (size <= byte.MaxValue)
                {
                    _out.Write(STRUCT_8, (byte) size, signature);
                }
                else if (size <= short.MaxValue)
                {
                    _out.Write(STRUCT_16, BitConverter.GetBytes((short) size)).Write(signature);
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(size), size,
                        $"Structures cannot have more than {short.MaxValue} fields");
            }
        }

        public class Unpacker
        {
            private readonly IInputStream _in;
            private static readonly BitConverterBase BitConverter = SocketClient.BitConverter;

            public Unpacker(IInputStream inputStream)
            {
                _in = inputStream;
            }

            public object UnpackNull()
            {
                byte markerByte = _in.ReadByte();
                if (markerByte != NULL)
                {
                    throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                        $"Expected a null, but got: 0x{(markerByte & 0xFF).ToString("X2")}");
                }
                return null;
            }

            public bool UnpackBoolean()
            {
                byte markerByte = _in.ReadByte();
                switch (markerByte)
                {
                    case TRUE:
                        return true;
                    case FALSE:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected a boolean, but got: 0x{(markerByte & 0xFF).ToString("X2")}");
                }
            }

            public long UnpackLong()
            {
                byte markerByte = _in.ReadByte();
                if ((sbyte) markerByte >= MINUS_2_TO_THE_4)
                {
                    return (sbyte) markerByte;
                }
                switch (markerByte)
                {
                    case INT_8:
                        return _in.ReadSByte();
                    case INT_16:
                        return _in.ReadShort();
                    case INT_32:
                        return _in.ReadInt();
                    case INT_64:
                        return _in.ReadLong();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected an integer, but got: 0x{markerByte.ToString("X2")}");
                }
            }

            public double UnpackDouble()
            {
                byte markerByte = _in.ReadByte();
                if (markerByte == FLOAT_64)
                {
                    return _in.ReadDouble();
                }
                throw new ArgumentOutOfRangeException( nameof(markerByte), markerByte,
                    $"Expected a double, but got: 0x{markerByte.ToString("X2")}");
            }

            public string UnpackString()
            {
                var markerByte = _in.ReadByte();
                if (markerByte == TINY_STRING) // Note no mask, so we compare to 0x80.
                {
                    return string.Empty;
                }

                return BitConverter.ToString(UnpackUtf8(markerByte));
            }

            public byte[] UnpackBytes()
            {
                byte markerByte = _in.ReadByte();

                switch (markerByte)
                {
                    case BYTES_8:
                        return UnpackBytes(UnpackUint8());
                    case BYTES_16:
                        return UnpackBytes(UnpackUint16());
                    case BYTES_32:
                    {
                        long size = UnpackUint32();
                        if (size <= int.MaxValue)
                        {
                            return UnpackBytes((int) size);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException(nameof(size), size,
                                $"BYTES_32 {size} too long for PackStream");
                        }
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected binary data, but got: 0x{(markerByte & 0xFF).ToString("X2")}");
                }
            }

            private byte[] UnpackBytes(int size)
            {
                var heapBuffer = new byte[size];
                _in.ReadBytes(heapBuffer);
                return heapBuffer;
            }

            private byte[] UnpackUtf8(byte markerByte)
            {
                var markerHighNibble = (byte) (markerByte & 0xF0);
                var markerLowNibble = (byte) (markerByte & 0x0F);

                if (markerHighNibble == TINY_STRING)
                {
                    return UnpackBytes(markerLowNibble);
                }
                switch (markerByte)
                {
                    case STRING_8:
                        return UnpackBytes(UnpackUint8());
                    case STRING_16:
                        return UnpackBytes(UnpackUint16());
                    case STRING_32:
                    {
                        var size = UnpackUint32();
                        if (size <= int.MaxValue)
                        {
                            return UnpackBytes((int) size);
                        }
                        throw new ArgumentOutOfRangeException(nameof(size), size, 
                            $"STRING_32 {size} too long for PackStream");
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected a string, but got: 0x{(markerByte & 0xFF).ToString("X2")}");
                }
            }

            public long UnpackMapHeader()
            {
                var markerByte = _in.ReadByte();
                var markerHighNibble = (byte) (markerByte & 0xF0);
                var markerLowNibble = (byte) (markerByte & 0x0F);

                if (markerHighNibble == TINY_MAP)
                {
                    return markerLowNibble;
                }
                switch (markerByte)
                {
                    case MAP_8:
                        return UnpackUint8();
                    case MAP_16:
                        return UnpackUint16();
                    case MAP_32:
                        return UnpackUint32();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected a map, but got: {markerByte.ToString("X2")}");
                }
            }

            public long UnpackListHeader()
            {
                var markerByte = _in.ReadByte();
                var markerHighNibble = (byte)(markerByte & 0xF0);
                var markerLowNibble = (byte)(markerByte & 0x0F);

                if (markerHighNibble == TINY_LIST)
                {
                    return markerLowNibble;
                }
                switch (markerByte)
                {
                    case LIST_8:
                        return UnpackUint8();
                    case LIST_16:
                        return UnpackUint16();
                    case LIST_32:
                        return UnpackUint32();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected a list, but got: {(markerByte & 0xFF).ToString("X2")}");
                }
            }

            public byte UnpackStructSignature()
            {
                return _in.ReadByte();
            }

            public long UnpackStructHeader()
            {
                var markerByte = _in.ReadByte();
                var markerHighNibble = (byte) (markerByte & 0xF0);
                var markerLowNibble = (byte) (markerByte & 0x0F);

                if (markerHighNibble == TINY_STRUCT)
                {
                    return markerLowNibble;
                }
                switch (markerByte)
                {
                    case STRUCT_8:
                        return UnpackUint8();
                    case STRUCT_16:
                        return UnpackUint16();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Expected a struct, but got: {markerByte.ToString("X2")}");
                }
            }

            public PackType PeekNextType()
            {
                var markerByte = _in.PeekByte();
                var markerHighNibble = (byte)(markerByte & 0xF0);

                switch (markerHighNibble)
                {
                    case TINY_STRING:
                        return PackType.String;
                    case TINY_LIST:
                        return PackType.List;
                    case TINY_MAP:
                        return PackType.Map;
                    case TINY_STRUCT:
                        return PackType.Struct;
                }

                if ((sbyte)markerByte >= MINUS_2_TO_THE_4)
                    return PackType.Integer;

                switch (markerByte)
                {
                    case NULL:
                        return PackType.Null;
                    case TRUE:
                    case FALSE:
                        return PackType.Boolean;
                    case FLOAT_64:
                        return PackType.Float;
                    case BYTES_8:
                    case BYTES_16:
                    case BYTES_32:
                        return PackType.Bytes;
                    case STRING_8:
                    case STRING_16:
                    case STRING_32:
                        return PackType.String;
                    case LIST_8:
                    case LIST_16:
                    case LIST_32:
                        return PackType.List;
                    case MAP_8:
                    case MAP_16:
                    case MAP_32:
                        return PackType.Map;
                    case STRUCT_8:
                    case STRUCT_16:
                        return PackType.Struct;
                    case INT_8:
                    case INT_16:
                    case INT_32:
                    case INT_64:
                        return PackType.Integer;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(markerByte), markerByte,
                            $"Unknown type 0x{markerByte.ToString("X2")}");
                }
            }

            private int UnpackUint8()
            {
                return _in.ReadByte() & 0xFF;
            }

            private int UnpackUint16()
            {
                return _in.ReadShort() & 0xFFFF;
            }

            private long UnpackUint32()
            {
                return _in.ReadInt() & 0xFFFFFFFFL;
            }

        }
    }

    internal interface IWriter
    {
        void Write(IRequestMessage requestMessage);
        void Flush();
    }

    internal interface IReader
    {
//        bool HasNext();
        void Read(IMessageResponseHandler responseHandler);
    }
}