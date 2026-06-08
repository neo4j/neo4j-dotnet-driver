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

using System.Collections.Generic;

namespace Neo4j.Driver.Bolt.PackStream.Abstractions;

/// <summary>
/// Writes PackStream-typed values to an underlying byte sink. Implementations own marker choice,
/// endianness, and UTF-8 encoding. Callers use this from message encoders; buffering lives below the implementation.
/// </summary>
internal interface IPackStreamWriter
{
    void WriteNull();

    void WriteBoolean(bool value);

    /// <summary>Writes the smallest suitable PackStream integer encoding.</summary>
    void WriteInteger(long value);

    void WriteFloat64(double value);

    /// <summary>Writes a PackStream TEXT value. <paramref name="value"/> must not be null; use <see cref="WriteNull"/> for null.</summary>
    void WriteString(string value);

    /// <summary>Writes a PackStream TEXT value from UTF-8 bytes (no conversion).</summary>
    void WriteUtf8String(ReadOnlySpan<byte> utf8);

    void WriteBytes(ReadOnlySpan<byte> value);

    /// <summary>
    /// Writes a PackStream list: marker, length, then each element via the same rules as <see cref="WriteObject"/>.
    /// </summary>
    void WriteList(IReadOnlyList<object?> items);

    /// <summary>
    /// Writes a PackStream map with string keys. Values are written via <see cref="WriteObject"/>.
    /// </summary>
    void WriteMap(IReadOnlyDictionary<string, object?> map);

    /// <summary>
    /// Writes the struct marker, field count, and signature byte (<paramref name="tag"/>).
    /// The caller must then write exactly <paramref name="fieldCount"/> PackStream values in order.
    /// </summary>
    void WriteStructHeader(byte tag, int fieldCount);

    /// <summary>
    /// Writes a value suitable as a map entry, list element, or struct field: null, bool, integral types,
    /// <see cref="float"/>/<see cref="double"/>, <see cref="string"/>, <see cref="byte"/> arrays,
    /// nested <see cref="IReadOnlyDictionary{TKey,TValue}"/> (string keys), or nested <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <exception cref="System.NotSupportedException">The runtime type of <paramref name="value"/> is not supported.</exception>
    void WriteObject(object? value);
}
