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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Neo4j.Driver.Internal.Messaging;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO;

internal sealed class PackStreamWriter
{
    private readonly MessageFormat _format;
    private readonly Stream _stream;

    public PackStreamWriter(MessageFormat format, Stream stream)
    {
        _format = format;
        _stream = stream;
    }

    public void Write(object value)
    {
        switch (value)
        {
            case null:
                WriteNull();
                break;

            case bool boolValue:
                WriteBool(boolValue);
                break;

            case sbyte sbyteValue:
                WriteLong(Convert.ToInt64(sbyteValue));
                break;

            case byte byteValue:
                WriteLong(Convert.ToInt64(byteValue));
                break;

            case short shortValue:
                WriteLong(Convert.ToInt64(shortValue));
                break;

            case int intValue:
                WriteLong(Convert.ToInt64(intValue));
                break;

            case long longValue:
                WriteLong(longValue);
                break;

            case byte[] bytes:
                WriteByteArray(bytes);
                break;

            case double doubleValue:
                WriteDouble(doubleValue);
                break;

            case float floatValue:
                WriteDouble(Convert.ToDouble(floatValue));
                break;

            case decimal decimalValue:
                WriteDouble(Convert.ToDouble(decimalValue));
                break;

            case char charValue:
                WriteChar(charValue);
                break;

            case string stringValue:
                WriteString(stringValue);
                break;

            case IList list:
                WriteList(list);
                break;

            case IDictionary dictionary:
                WriteDictionary(dictionary);
                break;

            case IEnumerable enumerable:
                WriteEnumerable(enumerable);
                break;

            case IMessage message:
                WriteMessage(message);
                break;

            default:
                if (_format.WriteStructHandlers.TryGetValue(value.GetType(), out var structHandler))
                {
                    structHandler.Serialize(_format.Version, this, value);
                }
                else
                {
                    throw new ProtocolException(
                        $"Cannot understand {nameof(value)} with type {value.GetType().FullName}");
                }

                break;
        }
    }

    private void WriteMessage(IMessage message)
    {
        message.Serializer.Serialize(_format.Version, this, message);
    }

    public void WriteInt(int value)
    {
        WriteLong(value);
    }

    public void WriteEnumerable(IEnumerable value)
    {
        var list = new List<object>();
        foreach (var item in value)
        {
            list.Add(item);
        }

        WriteList(list);
    }

    public void WriteLong(long value)
    {
        if (value >= Minus2ToThe4 && value < Plus2ToThe7)
        {
            _stream.WriteByte((byte)value);
        }
        else if (value is >= Minus2ToThe7 and < Minus2ToThe4)
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

    public void WriteDouble(double value)
    {
        _stream.WriteByte(Float64);
        _stream.Write(PackStreamBitConverter.GetBytes(value));
    }

    public void WriteBool(bool value)
    {
        _stream.WriteByte(value ? True : False);
    }

    public void WriteChar(char value)
    {
        WriteString(value.ToString());
    }

    public void WriteString(string value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            var bytes = PackStreamBitConverter.GetBytes(value);
            WriteStringHeader(bytes.Length);
            _stream.Write(bytes);
        }
    }

    public void WriteByteArray(byte[] values)
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

    public void WriteList(IList value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            WriteListHeader(value.Count);
            foreach (var item in value)
            {
                Write(item);
            }
        }
    }

    public void WriteDictionary(IDictionary values)
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

    public void WriteDictionary(IDictionary<string, string> values)
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
                WriteString(key);
                Write(values[key]);
            }
        }
    }

    public void WriteDictionary(IDictionary<string, object> values)
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
                WriteString(key);
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
            _stream.Write(new[] { (byte)size });
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

    public void WriteListHeader(int size)
    {
        if (size < 0x10)
        {
            _stream.WriteByte((byte)(TinyList | size));
            _stream.Write(new byte[0]);
        }
        else if (size <= byte.MaxValue)
        {
            _stream.WriteByte(List8);
            _stream.Write(new[] { (byte)size });
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

    public void WriteMapHeader(int size)
    {
        if (size < 0x10)
        {
            _stream.WriteByte((byte)(TinyMap | size));
            _stream.Write(new byte[0]);
        }
        else if (size <= byte.MaxValue)
        {
            _stream.WriteByte(Map8);
            _stream.Write(new[] { (byte)size });
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
            _stream.Write(new[] { (byte)size });
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

    public void WriteStructHeader(int size, byte signature)
    {
        if (size < 0x10)
        {
            _stream.WriteByte((byte)(TinyStruct | size));
            _stream.Write(new[] { signature });
        }
        else if (size <= byte.MaxValue)
        {
            _stream.WriteByte(Struct8);
            _stream.Write(new[] { (byte)size, signature });
        }
        else if (size <= short.MaxValue)
        {
            _stream.WriteByte(Struct16);
            _stream.Write(PackStreamBitConverter.GetBytes((short)size));
            _stream.WriteByte(signature);
        }
        else
        {
            throw new ProtocolException($"Structures cannot have more than {short.MaxValue} fields");
        }
    }
}
