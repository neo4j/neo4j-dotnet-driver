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
using Neo4j.Driver.Internal.Types;

namespace Neo4j.Driver;

/// <summary>
/// Represents an abstract base class for a mathematical vector with elements of supported numeric types.
/// </summary>
/// <remarks>
/// Supported element types include: <see cref="float"/>, <see cref="double"/>, <see cref="sbyte"/>, <see cref="short"/>, 
/// <see cref="int"/>, and <see cref="long"/>.
/// </remarks>
public abstract class Vector : IValue, IEquatable<Vector>
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(float),  // f32
        typeof(double), // f64
        typeof(sbyte),  // i8
        typeof(short),  // i16
        typeof(int),    // i32
        typeof(long)    // i64
    ];

    /// <summary>
    /// Determines whether the specified type is supported for use as a vector element.
    /// </summary>
    /// <param name="type">The type to check for support.</param>
    /// <returns>
    /// <c>true</c> if the specified type is supported; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSupported(Type type)
    {
        return SupportedTypes.Contains(type);
    }

    /// <summary>
    /// Gets the elements of the vector as an array of objects, regardless of their underlying type.
    /// </summary>
    public abstract object[] UntypedValues { get; }

     /// <summary>
    /// Creates a new <see cref="Vector{T}"/> instance from the specified collection of values.
    /// </summary>
    /// <typeparam name="T">The type of the vector elements. Must be a supported numeric type.</typeparam>
    /// <param name="values">The collection of values to initialize the vector with.</param>
    /// <returns>A new <see cref="Vector{T}"/> containing the specified values.</returns>
    /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not a supported type.</exception>
    public static Vector<T> Create<T>(IEnumerable<object> values) where T : struct
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported for Vector.");
        }

        return new Vector<T>(values.Cast<T>().ToArray());
    }

    /// <summary>
    /// Gets the number of elements in the vector.
    /// </summary>
    public int Length => UntypedValues.Length;

    /// <inheritdoc />
    public bool Equals(Vector other)
    {
        return other != null && UntypedValues.SequenceEqual(other.UntypedValues);
    }
}

/// <summary>
/// Represents a mathematical vector with elements of a specific supported numeric type.
/// </summary>
/// <typeparam name="T">
/// The type of the vector elements. Must be one of the supported numeric types: <see cref="float"/>, <see cref="double"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="int"/>, or <see cref="long"/>.
/// </typeparam>
public class Vector<T> : Vector, IEquatable<Vector<T>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Vector{T}"/> class.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown if <typeparamref name="T"/> is not a supported numeric type.
    /// </exception>
    public Vector()
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported for Vector.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector{T}"/> class with the specified values.
    /// </summary>
    /// <param name="values">The array of values to initialize the vector with. Must not be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> is null or empty.</exception>
    /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not a supported numeric type.</exception>
    public Vector(T[] values) : this()
    {
        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Values cannot be null or empty.", nameof(values));
        }

        Values = values;
    }

    /// <summary>
    /// Gets the array of values contained in the vector.
    /// </summary>
    public T[] Values { get; }

    /// <inheritdoc />
    public override object[] UntypedValues => Values.Select(v => (object)v).ToArray();

    /// <inheritdoc />
    public bool Equals(Vector<T> other)
    {
        return other != null && Values.SequenceEqual(other.Values);
    }
}
