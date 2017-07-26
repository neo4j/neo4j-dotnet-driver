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

        private readonly byte[] _byteBuffer = new byte[1];
        private readonly byte[] _shortBuffer = new byte[2];
        private readonly byte[] _intBuffer = new byte[4];
        private readonly byte[] _longBuffer = new byte[8];

        private readonly Stream _stream;

        public PackStreamReader(Stream stream)
        {
            Throw.ArgumentNullException.IfNull(stream, nameof(stream));
            Throw.ArgumentOutOfRangeException.IfFalse(stream.CanRead, nameof(stream));

            _stream = stream;
        }

        public object Read()
        {
            var result = ReadValue();

            return result;
        }

        private Dictionary<string, object> ReadMap()
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
                map.Add(key, ReadValue());
            }
            return map;
        }

        private IList<object> ReadList()
        {
            var size = (int)ReadListHeader();
            var vals = new object[size];
            for (var j = 0; j < size; j++)
            {
                vals[j] = ReadValue();
            }
            return new List<object>(vals);
        }

        private object ReadValue()
        {
            var type = PeekNextType();
            return ReadValue(type);
        }

        protected virtual object ReadValue(PackStream.PackType type)
        {
            switch (type)
            {
                case PackStream.PackType.Bytes:
                    return ReadBytes();
                case PackStream.PackType.Null:
                    return ReadNull();
                case PackStream.PackType.Boolean:
                    return ReadBoolean();
                case PackStream.PackType.Integer:
                    return ReadLong();
                case PackStream.PackType.Float:
                    return ReadDouble();
                case PackStream.PackType.String:
                    return ReadString();
                case PackStream.PackType.Map:
                    return ReadMap();
                case PackStream.PackType.List:
                    return ReadList();
                case PackStream.PackType.Struct:
                    return ReadStructure();
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown value type: {type}");
        }


        private Structure ReadStructure()
        {
            long size = ReadStructHeader();
            byte type = ReadStructSignature();

            List<object> fields = new List<object>();
            for (int i = 0; i < size; i++)
            {
                fields.Add(ReadValue());
            }

            return new Structure(type, fields);
        }

        public object ReadNull()
        {
            byte markerByte = NextByte();
            if (markerByte != NULL)
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
                case TRUE:
                    return true;
                case FALSE:
                    return false;
                default:
                    throw new ProtocolException(
                        $"Expected a boolean, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public long ReadLong()
        {
            byte markerByte = NextByte();
            if ((sbyte)markerByte >= MINUS_2_TO_THE_4)
            {
                return (sbyte)markerByte;
            }
            switch (markerByte)
            {
                case INT_8:
                    return NextSByte();
                case INT_16:
                    return NextShort();
                case INT_32:
                    return NextInt();
                case INT_64:
                    return NextLong();
                default:
                    throw new ProtocolException(
                        $"Expected an integer, but got: 0x{markerByte:X2}");
            }
        }

        public double ReadDouble()
        {
            byte markerByte = NextByte();
            if (markerByte == FLOAT_64)
            {
                return NextDouble();
            }
            throw new ProtocolException(
                $"Expected a double, but got: 0x{markerByte:X2}");
        }

        public string ReadString()
        {
            var markerByte = NextByte();
            if (markerByte == TINY_STRING) // Note no mask, so we compare to 0x80.
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
                case BYTES_8:
                    return ReadBytes(ReadUint8());
                case BYTES_16:
                    return ReadBytes(ReadUint16());
                case BYTES_32:
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
            var heapBuffer = new byte[size];
            _stream.Read(heapBuffer);
            return heapBuffer;
        }

        private byte[] ReadUtf8(byte markerByte)
        {
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

            if (markerHighNibble == TINY_STRING)
            {
                return ReadBytes(markerLowNibble);
            }
            switch (markerByte)
            {
                case STRING_8:
                    return ReadBytes(ReadUint8());
                case STRING_16:
                    return ReadBytes(ReadUint16());
                case STRING_32:
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

            if (markerHighNibble == TINY_MAP)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case MAP_8:
                    return ReadUint8();
                case MAP_16:
                    return ReadUint16();
                case MAP_32:
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

            if (markerHighNibble == TINY_LIST)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case LIST_8:
                    return ReadUint8();
                case LIST_16:
                    return ReadUint16();
                case LIST_32:
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

            if (markerHighNibble == TINY_STRUCT)
            {
                return markerLowNibble;
            }
            switch (markerByte)
            {
                case STRUCT_8:
                    return ReadUint8();
                case STRUCT_16:
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
            var pos = _stream.Position;
            try
            {
                if (_stream.Length - _stream.Position < 1)
                {
                    throw new ProtocolException("Unable to peek 1 byte from buffer.");
                }

                return (byte)_stream.ReadByte();
            }
            finally
            {
                _stream.Position = pos;
            }
        }

    }
}
