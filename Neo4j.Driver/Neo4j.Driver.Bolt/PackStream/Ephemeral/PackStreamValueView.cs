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

using System.Buffers;
using Neo4j.Driver.Bolt.PackStream.Abstractions;

namespace Neo4j.Driver.Bolt.PackStream.Ephemeral;

public readonly struct PackStreamValueView
{
    private readonly PackStreamType _type;
    private readonly long? _intValue;
    private readonly double? _floatValue;
    private readonly bool? _boolValue;
    private readonly ReadOnlySequence<byte>? _bytesValue;
    private readonly ReadOnlySequence<byte>? _stringBytes;
    private readonly PackStreamListView? _listValue;
    private readonly PackStreamMapView? _mapValue;
    private readonly PackStreamStructView? _structValue;

    private PackStreamValueView(
        PackStreamType type,
        long? intValue = null,
        double? floatValue = null,
        bool? boolValue = null,
        ReadOnlySequence<byte>? bytesValue = null,
        ReadOnlySequence<byte>? stringBytes = null,
        PackStreamListView? listValue = null,
        PackStreamMapView? mapValue = null,
        PackStreamStructView? structValue = null)
    {
        _type = type;
        _intValue = intValue;
        _floatValue = floatValue;
        _boolValue = boolValue;
        _bytesValue = bytesValue;
        _stringBytes = stringBytes;
        _listValue = listValue;
        _mapValue = mapValue;
        _structValue = structValue;
    }

    public PackStreamType Type
    {
        get { return _type; }
    }

    public bool IsNull
    {
        get { return _type == PackStreamType.Null; }
    }

    private T ReadOrThrow<T>(T? value, string name) where T : struct
    {
        return value ?? throw new InvalidOperationException($"Cannot read {name} from {_type}");
    }

    public long IntValue
    {
        get { return ReadOrThrow(_intValue, nameof(IntValue)); }
    }

    public static PackStreamValueView Integer(long value)
    {
        return new PackStreamValueView(PackStreamType.Integer, intValue: value);
    }

    public double FloatValue
    {
        get { return ReadOrThrow(_floatValue, nameof(FloatValue)); }
    }

    public static PackStreamValueView Float(double value)
    {
        return new PackStreamValueView(PackStreamType.Float, floatValue: value);
    }

    public bool BooleanValue
    {
        get { return ReadOrThrow(_boolValue, nameof(BooleanValue)); }
    }

    public static PackStreamValueView Boolean(bool value)
    {
        return new PackStreamValueView(PackStreamType.Boolean, boolValue: value);
    }

    public ReadOnlySequence<byte> BytesValue
    {
        get { return _bytesValue ?? throw new InvalidOperationException($"Cannot read BytesValue from {_type}"); }
    }

    public static PackStreamValueView Bytes(ReadOnlySequence<byte> value)
    {
        return new PackStreamValueView(PackStreamType.Bytes, bytesValue: value);
    }

    public PackStreamStringView StringValue
    {
        get
        {
            return _stringBytes.HasValue
                ? new PackStreamStringView(_stringBytes.Value)
                : throw new InvalidOperationException($"Cannot read StringValue from {_type}");
        }
    }

    public static PackStreamValueView String(ReadOnlySequence<byte> utf8Bytes)
    {
        return new PackStreamValueView(PackStreamType.String, stringBytes: utf8Bytes);
    }

    public PackStreamListView ListValue
    {
        get { return _listValue ?? throw new InvalidOperationException($"Cannot read ListValue from {_type}"); }
    }

    internal static PackStreamValueView List(
        ReadOnlySequence<byte> itemsData,
        int itemCount,
        IPackStreamDecoder decoder)
    {
        return new PackStreamValueView(
            PackStreamType.List,
            listValue: new PackStreamListView(itemsData, itemCount, decoder));
    }

    public PackStreamMapView MapValue
    {
        get { return _mapValue ?? throw new InvalidOperationException($"Cannot read MapValue from {_type}"); }
    }

    internal static PackStreamValueView Map(
        ReadOnlySequence<byte> entriesData,
        int entryCount,
        IPackStreamDecoder decoder)
    {
        return new PackStreamValueView(
            PackStreamType.Map,
            mapValue: new PackStreamMapView(entriesData, entryCount, decoder));
    }

    public PackStreamStructView StructValue
    {
        get { return _structValue ?? throw new InvalidOperationException($"Cannot read StructValue from {_type}"); }
    }

    internal static PackStreamValueView Struct(PackStreamStructView structValue)
    {
        return new PackStreamValueView(PackStreamType.Struct, structValue: structValue);
    }

    public static PackStreamValueView Null()
    {
        return new PackStreamValueView(PackStreamType.Null);
    }

    public override string ToString()
    {
        return _type switch
        {
            PackStreamType.Integer => $"INT {_intValue}",
            PackStreamType.Float => $"FLOAT {_floatValue}",
            PackStreamType.Boolean => $"BOOL {_boolValue}",
            PackStreamType.Bytes => $"BYTES[{_bytesValue?.Length ?? 0}]",
            PackStreamType.String => $"STRING \"{StringValue.ToString()}\"",
            PackStreamType.List => $"LIST[{_listValue?.Count ?? 0}]",
            PackStreamType.Map => $"MAP[{_mapValue?.Count ?? 0}]",
            PackStreamType.Struct => $"STRUCT[0x{_structValue?.Tag:X2},{_structValue?.Fields.Count ?? 0}]",
            PackStreamType.Null => "NULL",
            _ => "UNKNOWN"
        };
    }
}
