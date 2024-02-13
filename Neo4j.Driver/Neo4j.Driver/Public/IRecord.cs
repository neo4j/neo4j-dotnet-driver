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

namespace Neo4j.Driver;

/// <summary>A record contains ordered key and value pairs</summary>
public interface IRecord : IReadOnlyDictionary<string, object>
{
    /// <summary>Gets the value at the given index.</summary>
    /// <param name="index">The index</param>
    /// <returns>The value specified with the given index.</returns>
    object this[int index] { get; }

    /// <summary>Gets the value specified by the given key.</summary>
    /// <param name="key">The key</param>
    /// <returns>the value specified with the given key.</returns>
    new object this[string key] { get; }

    /// <summary>Gets the value specified by the given key and converts it to the given type.</summary>
    /// <param name="key">The key</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    T Get<T>(string key);

    /// <summary>
    /// Tries to get the value specified by the given key and converts it to the given type.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value, if the key was found.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns><c>true</c> if the value is found; <c>false</c> otherwise.</returns>
    bool TryGet<T> (string key, out T value);

    /// <summary>Gets the value specified by the given key and converts it to the given type.</summary>
    /// <param name="key">The key</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    T GetCaseInsensitive<T>(string key);

    /// <summary>
    /// Tries to get the value specified by the given key and converts it to the given type.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value, if the key was found.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns><c>true</c> if the value is found; <c>false</c> otherwise.</returns>
    bool TryGetCaseInsensitive<T>(string key, out T value);

    /// <summary>
    /// Tries to get the value specified by the given key in a case-insensitive manner.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value specified with the given key.</param>
    /// <returns><c>true</c> if the value is found; <c>false</c> otherwise.</returns>
    bool TryGetValueByCaseInsensitiveKey(string key, out object value);

    /// <summary>Gets the key and value pairs in a <see cref="IReadOnlyDictionary{TKey,TValue}"/>.</summary>
    new IReadOnlyDictionary<string, object> Values { get; }

    /// <summary>Gets the keys in a <see cref="IReadOnlyList{T}"/>.</summary>
    new IReadOnlyList<string> Keys { get; }
}
