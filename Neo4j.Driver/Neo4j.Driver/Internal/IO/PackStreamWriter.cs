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
            else
            {
                throw new ProtocolException(
                    $"Cannot understand {nameof(value)} with type {value.GetType().FullName}");
            }
        }

        public void Write(long value)
        {
            if (value >= Minus2ToThe4 && value < Plus2ToThe7)
            {
                _stream.WriteByte((byte)value);
            }
            else if (value >= Minus2ToThe7 && value < Minus2ToThe4)
            {
                _stream.WriteByte(Int8);
                _stream.Write(PackStreamBitConverter.GetBytes((byte)value));
            }
            else if (value >= Minus2ToThe15 && value < Plus2ToThe15)
            {
                _stream.WriteByte(PackStream.Int16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)value));
            }
            else if (value >= Minus2ToThe31 && value < Plus2ToThe31)
            {
                _stream.WriteByte(PackStream.Int32);
                _stream.Write(PackStreamBitConverter.GetBytes((int)value));
            }
            else
            {
                _stream.WriteByte(PackStream.Int64);
                _stream.Write(PackStreamBitConverter.GetBytes(value));
            }
        }

        public void Write(double value)
        {
            _stream.WriteByte(Float64);
            _stream.Write(PackStreamBitConverter.GetBytes(value));
        }

        public void Write(bool value)
        {
            _stream.WriteByte(value ? True : False);
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
        
        public void WriteNull()
        {
            _stream.WriteByte(Null);
        }

        private void WriteRaw(byte[] data)
        {
            _stream.Write(data);
        }

        private void WriteBytesHeader(int size)
        {
            if (size <= byte.MaxValue)
            {
                _stream.WriteByte(Bytes8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(Bytes16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(Bytes32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteListHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TinyList | size));
                _stream.Write(new byte[0]);
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(List8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(List16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(List32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteMapHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TinyMap | size));
                _stream.Write(new byte[0]);
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(Map8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(Map16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(Map32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        private void WriteStringHeader(int size)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TinyString | size));
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(String8);
                _stream.Write(new byte[] {(byte) size});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(String16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            }
            else
            {
                _stream.WriteByte(String32);
                _stream.Write(PackStreamBitConverter.GetBytes(size));
            }
        }

        internal void WriteStructHeader(int size, byte signature)
        {
            if (size < 0x10)
            {
                _stream.WriteByte((byte)(TinyStruct | size));
                _stream.Write(new byte[] { signature });
            }
            else if (size <= byte.MaxValue)
            {
                _stream.WriteByte(Struct8);
                _stream.Write(new byte[] {(byte) size, signature});
            }
            else if (size <= short.MaxValue)
            {
                _stream.WriteByte(Struct16);
                _stream.Write(PackStreamBitConverter.GetBytes((short)size));
                _stream.WriteByte(signature);
            }
            else
                throw new ProtocolException(
                    $"Structures cannot have more than {short.MaxValue} fields");
        }

    }
}
