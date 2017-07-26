using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class PackStreamWriter: IPackStreamWriter
    {
        private readonly Stream _stream;

        public PackStreamWriter(Stream stream)
        {
            Throw.ArgumentNullException.IfNull(stream, nameof(stream));
            Throw.ArgumentOutOfRangeException.IfFalse(stream.CanWrite, nameof(stream));

            _stream = stream;
        }

        public void Write(object value)
        {
            if (value == null)
            {
                WriteNull();
            }
            else if (value is bool)
            {
                Write((bool)value);
            }

            else if (value is sbyte || value is byte || value is short || value is int || value is long)
            {
                Write(Convert.ToInt64(value));
            }
            else if (value is byte[])
            {
                Write((byte[])value);
            }
            else if (value is float || value is double || value is decimal)
            {
                Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
            else if (value is char || value is string)
            {
                Write(value.ToString());
            }
            else if (value is IList)
            {
                Write((IList)value);
            }
            else if (value is IDictionary)
            {
                Write((IDictionary)value);
            }
            else if (value is Structure)
            {
                Write((Structure)value);    
            }
            else
            {
                throw new ProtocolException(
                    $"Cannot understand {nameof(value)} with type {value.GetType().FullName}");
            }
        }

        public void Write(long value)
        {
            if (value >= MINUS_2_TO_THE_4 && value < PLUS_2_TO_THE_7)
            {
                _stream.WriteByte((byte)value);
            }
            else if (value >= MINUS_2_TO_THE_7 && value < MINUS_2_TO_THE_4)
            {
                _stream.WriteByte(INT_8);
                _stream.Write(PackStreamBitConverter.GetBytes((byte)value));
            }
            else if (value >= MINUS_2_TO_THE_15 && value < PLUS_2_TO_THE_15)
            {
                _stream.WriteByte(INT_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)value));
            }
            else if (value >= MINUS_2_TO_THE_31 && value < PLUS_2_TO_THE_31)
            {
                _stream.WriteByte(INT_32);
                _stream.Write(PackStreamBitConverter.GetBytes((int)value));
            }
            else
            {
                _stream.WriteByte(INT_64);
                _stream.Write(PackStreamBitConverter.GetBytes(value));
            }
        }

        public void Write(double value)
        {
            _stream.WriteByte(FLOAT_64);
            _stream.Write(PackStreamBitConverter.GetBytes(value));
        }

        public void Write(bool value)
        {
            _stream.WriteByte(value ? TRUE : FALSE);
        }

        public void Write(string value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            var bytes = PackStreamBitConverter.GetBytes(value);
            WriteStringHeader(bytes.Length);
            _stream.Write(bytes);
        }

        public virtual void Write(byte[] values)
        {
            if (values == null)
            {
                WriteNull();
            }
            else
            {
                WriteBytesHeader(values.Length);
                WriteRaw(values);
            }
        }

        public void Write(IList value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }
            WriteListHeader(value.Count);
            foreach (var item in value)
            {
                Write(item);
            }
        }

        public void Write(IDictionary values)
        {
            if (values == null)
            {
                WriteNull();
            }
            else
            {
                WriteMapHeader(values.Count);
                foreach (var key in values.Keys)
                {
                    Write(key);
                    Write(values[key]);
                }
            }
        }

        public void Write(Structure value)
        {
            if (value == null)
            {
                WriteNull();
            }
            else
            {
                WriteStructHeader(value.Fields.Count, value.Type);
                foreach (var obj in value.Fields)
                {
                    Write(obj);
                }
            }
        }

        public void WriteNull()
        {
            _stream.WriteByte(NULL);
        }

        private void WriteRaw(byte[] data)
        {
            _stream.Write(data);
        }

        private void WriteBytesHeader(int size)
        {
            if (size <= byte.MaxValue)
            {
                _stream.WriteByte(BYTES_8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(BYTES_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(BYTES_32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteListHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TINY_LIST | size));
                _stream.Write(new byte[0]);
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(LIST_8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(LIST_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(LIST_32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteMapHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TINY_MAP | size));
                _stream.Write(new byte[0]);
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(MAP_8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(MAP_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(MAP_32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        private void WriteStringHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TINY_STRING | size));
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(STRING_8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(STRING_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(STRING_32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteStructHeader(int size, byte signature)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TINY_STRUCT | size));
                _stream.Write(new byte[] { signature });
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(STRUCT_8);
                _stream.Write(new byte[] {(byte) size, signature});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(STRUCT_16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
                _stream.WriteByte(signature);
            }
            else
                throw new ProtocolException(
                    $"Structures cannot have more than {short.MaxValue} fields");
        }

    }
}
