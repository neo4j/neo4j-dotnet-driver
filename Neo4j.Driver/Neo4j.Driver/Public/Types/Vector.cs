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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>
/// An abstract base class for a mathematical vector with elements of supported numeric types.
/// </summary>
/// <remarks>
/// Supported element types are: <see cref="float"/>, <see cref="double"/>, <see cref="sbyte"/>, <see cref="short"/>,
/// <see cref="int"/>, and <see cref="long"/>.
/// </remarks>
public abstract class Vector : IValue, IVector, IEquatable<IVector>
{
    /// <summary>
    /// The set of supported types for vector elements. No other types are allowed.
    /// </summary>
    public static IEnumerable<Type> SupportedTypes => TypeNameMap.Keys;

    private static readonly Dictionary<Type, string> TypeNameMap = new()
    {
        [typeof(sbyte)] = "INTEGER8",
        [typeof(short)] = "INTEGER16",
        [typeof(int)] = "INTEGER32",
        [typeof(long)] = "INTEGER",
        [typeof(float)] = "FLOAT32",
        [typeof(double)] = "FLOAT",
    };

    /// <summary>
    /// Determines whether the specified type is supported for use as a vector element.
    /// </summary>
    /// <param name="type">The type to check for support.</param>
    /// <returns>
    /// <c>true</c> if the specified type is supported; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSupported(Type type)
    {
        return TypeNameMap.ContainsKey(type);
    }

    /// <summary>
    /// Gets the elements of the vector as an array of objects, regardless of their underlying type.
    /// </summary>
    public IEnumerable<object> UntypedValues { get; protected set; }

    /// <summary>
    /// Gets the original byte stream from which the vector was deserialized, if applicable.
    /// </summary>
    public byte[] OriginalByteStream { get; protected set; }

    /// <summary>
    /// Creates a new <see cref="Vector{T}"/> instance from the specified collection of values.
    /// </summary>
    /// <typeparam name="T">The type of the vector elements. Must be a supported numeric type.</typeparam>
    /// <param name="values">The collection of values to initialize the vector with.</param>
    /// <param name="originalByteStream">The original byte stream from which the vector was deserialized, if applicable.</param>
    /// <returns>A new <see cref="Vector{T}"/> containing the specified values.</returns>
    /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not a supported type.</exception>
    public static Vector<T> Create<T>(T[] values, byte[] originalByteStream = null) where T : struct
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported for Vector.");
        }

        return new Vector<T>(values, originalByteStream);
    }

    private static readonly MethodInfo CreateMethodInfo = typeof(Vector).GetMethod(nameof(Create));

    internal static Vector CreateDynamic(Array values, byte[] originalByteStream = null)
    {
        var elementType = values.GetType().GetElementType()!;
        if (!IsSupported(elementType))
        {
            throw new NotSupportedException($"Type {elementType.Name} is not supported for Vector.");
        }

        // Use reflection to call the generic Create<T> method
        var genericMethod = CreateMethodInfo.MakeGenericMethod(elementType);
        return (Vector)genericMethod.Invoke(null, [values, originalByteStream]);
    }

    internal static Vector CreateDynamic(IEnumerable<object> values, Type elementType, byte[] originalByteStream = null)
    {
        return CreateDynamic(values.Select(v => v.AsType(elementType)).ToArray(), originalByteStream);
    }

    /// <summary>
    /// Gets the type of the elements contained in the vector.
    /// </summary>
    public abstract Type ElementType { get; }

    /// <summary>
    /// Gets the number of elements in the vector.
    /// </summary>
    public abstract int Count { get; }

    /// <inheritdoc />
    public bool Equals(IVector other)
    {
        return other != null && UntypedValues.SequenceEqual(other.UntypedValues);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var elementType = GetTypeString(ElementType);
        var elements = string.Join(", ", UntypedValues.Select(FormatElement));
        return $"vector([{elements}], {Count}, {elementType} NOT NULL)";
    }

    private static string GetTypeString(Type type)
    {
        return IsSupported(type)
            ? TypeNameMap[type]
            : throw new NotSupportedException($"Type {type.Name} is not supported");
    }

    private static string FormatElement(object element)
    {
        return element switch
        {
            double d => FormatDouble(d),
            float f => FormatDouble(f),
            _ => element?.ToString() ?? "null"
        };
    }

    private static string FormatDouble(double value)
    {
        return value switch
        {
            double.NaN => "NaN",
            double.PositiveInfinity => "Infinity",
            double.NegativeInfinity => "-Infinity",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };
    }
}
