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

namespace Neo4j.Driver;

/// <summary>
/// Represents a mathematical vector with elements of supported numeric types.
/// </summary>
/// <remarks>
/// Supported element types are: <see cref="float"/>, <see cref="double"/>, <see cref="sbyte"/>, <see cref="short"/>,
/// <see cref="int"/>, and <see cref="long"/>.
/// </remarks>
public interface IVector
{
    /// Returns the elements of the vector as an array of objects, regardless of their underlying type.
    IEnumerable<object> UntypedValues { get; }

    /// Gets the original byte stream from which the vector was deserialized, if applicable.
    byte[] OriginalByteStream { get; }

    /// Gets the type of the elements contained in the vector.
    Type ElementType { get; }
}

/// <summary>
/// Represents a mathematical vector with elements of supported numeric types.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IVector<out T> : IEquatable<IVector>, IVector, IReadOnlyList<T>
    where T : struct
{
    /// <summary>
    /// Gets the array of values contained in the vector.
    /// </summary>
    IReadOnlyList<T> Values { get; }
}
