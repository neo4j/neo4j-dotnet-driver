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
using System.Linq;

namespace Neo4j.Driver;

/// <summary>
/// Represents a mathematical vector with elements of a specific supported numeric type.
/// </summary>
/// <typeparam name="T">
/// The type of the vector elements. Must be one of the supported numeric types: <see cref="float"/>,
/// <see cref="double"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="int"/>, or <see cref="long"/>.
/// </typeparam>
public class Vector<T> : Vector, IVector<T> where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Vector{T}"/> class.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown if <typeparamref name="T"/> is not a supported numeric type.
    /// </exception>
    public Vector()
    {
        EnsureSupported();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vector{T}"/> class with the specified values.
    /// </summary>
    /// <param name="values">The array of values to initialize the vector with. Must not be null or empty.</param>
    /// <param name="originalByteStream">The original byte stream from which the vector was deserialized, if applicable.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> is null or empty.</exception>
    /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> is not a supported numeric type.</exception>
    public Vector(T[] values, byte[] originalByteStream = null) : base(ToUntypedValues(values))
    {
        EnsureSupported();
        Values = values;
        OriginalByteStream = originalByteStream;
    }

    private static void EnsureSupported()
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported for Vector.");
        }
    }

    private static object[] ToUntypedValues(T[] values)
    {
        values = values ?? throw new ArgumentException("Values cannot be null.", nameof(values));
        return [..values.Select(x => (object)x)];
    }

    /// <summary>
    /// Gets the array of values contained in the vector.
    /// </summary>
    public IReadOnlyList<T> Values { get; }

    /// <inheritdoc />
    public override Type ElementType => typeof(T);

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    /// <inheritdoc cref="Count"/>
    public override int Count => Values.Count;

    /// <inheritdoc />
    public T this[int index] => Values[index];

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
