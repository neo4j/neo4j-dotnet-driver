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
using System.Linq;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.IO.ValueSerializers;

internal class VectorSerializer : IPackStreamSerializer
{
    public static VectorSerializer Instance { get; } = new ();

    private const byte VectorStructType = (byte)'V';
    private const int VectorStructSize = 2;

    /// <inheritdoc />
    public byte[] ReadableStructs => [VectorStructType];

    /// <inheritdoc />
    public IEnumerable<Type> WritableTypes => [typeof(Vector)];

    /// <inheritdoc />
    public object Deserialize(BoltProtocolVersion version, PackStreamReader reader, byte signature, long size)
    {
        if(signature != VectorStructType)
        {
            throw new ProtocolException(
                $"Unsupported struct signature {signature} passed to {nameof(VectorSerializer)}!");
        }

        PackStream.EnsureStructSize("Vector", VectorStructSize, size);
        var typeMarker = reader.ReadBytes()[0];
        if (!MarkerToType.TryGetValue(typeMarker, out var elementType))
        {
            throw new ProtocolException($"Unsupported vector element type marker 0x{typeMarker:X2}.");
        }

        var byteArray = reader.ReadBytes();
        var typedArray = BytesToTypedArrayHelper.ConvertBytesToTypedArray(byteArray, elementType);
        return Vector.CreateDynamic(typedArray, byteArray);
    }

    public static byte[] GetByteStream(IVector vector)
    {
        var byteConverter = GetByteConverter(vector.ElementType);
        var byteArray = vector.UntypedValues.Select(byteConverter).ToArray();
        var flattened = byteArray.SelectMany(b => b).ToArray();
        return flattened;
    }

    public void Serialize(BoltProtocolVersion version, PackStreamWriter writer, object value)
    {
        var vector = value.CastOrThrow<Vector>();
        writer.WriteStructHeader(VectorStructSize, VectorStructType);

        // the type marker is next
        writer.WriteByteArray([TypeToMarker[vector.ElementType]]);

        // then all the values
        var byteStream = GetByteStream(vector);
        writer.WriteByteArray(byteStream);
    }

    /// <inheritdoc />
    public (object, int) DeserializeSpan(BoltProtocolVersion version, SpanPackStreamReader reader, byte signature, int size)
    {
        if (signature != VectorStructType)
        {
            throw new ProtocolException(
                $"Unsupported struct signature {signature} passed to {nameof(VectorSerializer)}!");
        }

        PackStream.EnsureStructSize("Vector", VectorStructSize, size);
        var typeMarker = reader.ReadBytes()[0];
        if (!MarkerToType.TryGetValue(typeMarker, out var elementType))
        {
            throw new ProtocolException($"Unsupported vector element type marker 0x{typeMarker:X2}.");
        }

        var byteArray = reader.ReadBytes();
        var originalByteStream = byteArray.ToArray();
        var typedArray = BytesToTypedArrayHelper.ConvertBytesToTypedArray(byteArray, elementType);
        return (Vector.CreateDynamic(typedArray, originalByteStream), reader.Index);
    }

    private static readonly Dictionary<Type, byte> TypeToMarker = new()
    {
        { typeof(sbyte), PackStream.Int8 },
        { typeof(short), PackStream.Int16 },
        { typeof(int), PackStream.Int32 },
        { typeof(long), PackStream.Int64 },
        { typeof(float), PackStream.Float32 },
        { typeof(double), PackStream.Float64 }
    };

    private static readonly Dictionary<byte, Type> MarkerToType =
        TypeToMarker.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    private static Func<object, byte[]> GetByteConverter(Type type)
    {
        return type switch
        {
            _ when type == typeof(sbyte) => value => PackStreamBitConverter.GetBytes(unchecked((byte)(sbyte)value)),
            _ when type == typeof(short) => value => PackStreamBitConverter.GetBytes((short)value),
            _ when type == typeof(int) => value => PackStreamBitConverter.GetBytes((int)value),
            _ when type == typeof(long) => value => PackStreamBitConverter.GetBytes((long)value),
            _ when type == typeof(float) => value => PackStreamBitConverter.GetBytes((float)value),
            _ when type == typeof(double) => value => PackStreamBitConverter.GetBytes((double)value),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported vector element type {type}.")
        };
    }
}
