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
            var result = UnpackValue();

            return result;
        }

        private Dictionary<string, object> UnpackMap()
        {
            var size = (int)UnpackMapHeader();
            if (size == 0)
            {
                return EmptyStringValueMap;
            }
            var map = new Dictionary<string, object>(size);
            for (var i = 0; i < size; i++)
            {
                var key = UnpackString();
                map.Add(key, UnpackValue());
            }
            return map;
        }

        private IList<object> UnpackList()
        {
            var size = (int)UnpackListHeader();
            var vals = new object[size];
            for (var j = 0; j < size; j++)
            {
                vals[j] = UnpackValue();
            }
            return new List<object>(vals);
        }

        private object UnpackValue()
        {
            var type = PeekNextType();
            return UnpackValue(type);
        }

        protected virtual object UnpackValue(PackStream.PackType type)
        {
            switch (type)
            {
                case PackStream.PackType.Bytes:
                    return UnpackBytes();
                case PackStream.PackType.Null:
                    return UnpackNull();
                case PackStream.PackType.Boolean:
                    return UnpackBoolean();
                case PackStream.PackType.Integer:
                    return UnpackLong();
                case PackStream.PackType.Float:
                    return UnpackDouble();
                case PackStream.PackType.String:
                    return UnpackString();
                case PackStream.PackType.Map:
                    return UnpackMap();
                case PackStream.PackType.List:
                    return UnpackList();
                case PackStream.PackType.Struct:
                    return UnpackStructure();
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown value type: {type}");
        }


        private Structure UnpackStructure()
        {
            long size = UnpackStructHeader();
            byte type = UnpackStructSignature();

            List<object> fields = new List<object>();
            for (int i = 0; i < size; i++)
            {
                fields.Add(UnpackValue());
            }

            return new Structure(type, fields);
        }

        public object UnpackNull()
        {
            byte markerByte = ReadByte();
            if (markerByte != NULL)
            {
                throw new ProtocolException(
                    $"Expected a null, but got: 0x{(markerByte & 0xFF):X2}");
            }
            return null;
        }

        public bool UnpackBoolean()
        {
            byte markerByte = ReadByte();
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

        public long UnpackLong()
        {
            byte markerByte = ReadByte();
            if ((sbyte)markerByte >= MINUS_2_TO_THE_4)
            {
                return (sbyte)markerByte;
            }
            switch (markerByte)
            {
                case INT_8:
                    return ReadSByte();
                case INT_16:
                    return ReadShort();
                case INT_32:
                    return ReadInt();
                case INT_64:
                    return ReadLong();
                default:
                    throw new ProtocolException(
                        $"Expected an integer, but got: 0x{markerByte:X2}");
            }
        }

        public double UnpackDouble()
        {
            byte markerByte = ReadByte();
            if (markerByte == FLOAT_64)
            {
                return ReadDouble();
            }
            throw new ProtocolException(
                $"Expected a double, but got: 0x{markerByte:X2}");
        }

        public string UnpackString()
        {
            var markerByte = ReadByte();
            if (markerByte == TINY_STRING) // Note no mask, so we compare to 0x80.
            {
                return string.Empty;
            }

            return PackStreamBitConverter.ToString(UnpackUtf8(markerByte));
        }

        public virtual byte[] UnpackBytes()
        {
            byte markerByte = ReadByte();

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
                        return UnpackBytes((int)size);
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

        internal byte[] UnpackBytes(int size)
        {
            var heapBuffer = new byte[size];
            _stream.Read(heapBuffer);
            return heapBuffer;
        }

        private byte[] UnpackUtf8(byte markerByte)
        {
            var markerHighNibble = (byte)(markerByte & 0xF0);
            var markerLowNibble = (byte)(markerByte & 0x0F);

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
                        return UnpackBytes((int)size);
                    }
                    throw new ProtocolException(
                        $"STRING_32 {size} too long for PackStream");
                }
                default:
                    throw new ProtocolException(
                        $"Expected a string, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public long UnpackMapHeader()
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
                    return UnpackUint8();
                case MAP_16:
                    return UnpackUint16();
                case MAP_32:
                    return UnpackUint32();
                default:
                    throw new ProtocolException(
                        $"Expected a map, but got: 0x{markerByte:X2}");
            }
        }

        public long UnpackListHeader()
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
                    return UnpackUint8();
                case LIST_16:
                    return UnpackUint16();
                case LIST_32:
                    return UnpackUint32();
                default:
                    throw new ProtocolException(
                        $"Expected a list, but got: 0x{(markerByte & 0xFF):X2}");
            }
        }

        public byte UnpackStructSignature()
        {
            return ReadByte();
        }

        public long UnpackStructHeader()
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
                    return UnpackUint8();
                case STRUCT_16:
                    return UnpackUint16();
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

        private int UnpackUint8()
        {
            return ReadByte() & 0xFF;
        }

        private int UnpackUint16()
        {
            return ReadShort() & 0xFFFF;
        }

        private long UnpackUint32()
        {
            return ReadInt() & 0xFFFFFFFFL;
        }

        internal sbyte ReadSByte()
        {
            _stream.Read(_byteBuffer);

            return (sbyte)_byteBuffer[0];
        }

        public byte ReadByte()
        {
            _stream.Read(_byteBuffer);

            return (byte)_byteBuffer[0];
        }

        public short ReadShort()
        {
            _stream.Read(_shortBuffer);

            return PackStreamBitConverter.ToInt16(_shortBuffer);
        }

        public int ReadInt()
        {
            _stream.Read(_intBuffer);

            return PackStreamBitConverter.ToInt32(_intBuffer);
        }

        public long ReadLong()
        {
            _stream.Read(_longBuffer);

            return PackStreamBitConverter.ToInt64(_longBuffer);
        }

        public double ReadDouble()
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
