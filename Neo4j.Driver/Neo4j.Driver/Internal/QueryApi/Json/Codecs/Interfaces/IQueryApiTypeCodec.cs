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

#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Reads and writes a Neo4j value type to/from the HTTP Query API typed JSON envelope
/// (<c>{"$type":"...", "_value":...}</c>). Each codec owns its full envelope in both directions; the
/// dispatchers (<see cref="IJsonValueDecoder"/> for reads, <see cref="IJsonValueEncoder"/> for writes) only
/// select the codec that claims a given wire type name or CLR value. A codec may support only one direction
/// (e.g. result-only types are read-only).
/// </summary>
internal interface IQueryApiTypeCodec
{
    /// <summary>Whether this codec can read the given wire <c>$type</c> name.</summary>
    bool CanRead(string typeName);

    /// <summary>
    /// Reads a typed envelope element to a CLR value. <paramref name="element"/> is the full
    /// <c>{"$type":..., "_value":...}</c> object. <paramref name="recurse"/> converts nested values (lists/maps).
    /// </summary>
    object? Read(JsonElement element, IJsonValueDecoder recurse);

    /// <summary>Whether this codec can write the given CLR value.</summary>
    bool CanWrite(object? value);

    /// <summary>
    /// Encodes the value as a complete typed envelope node (<c>{"$type":"...","_value":...}</c>).
    /// <paramref name="recurse"/> encodes nested values (lists/maps).
    /// </summary>
    JsonNode? Write(object? value, IJsonValueEncoder recurse);
}
