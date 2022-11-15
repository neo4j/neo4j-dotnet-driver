// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
public interface IRecord
{
    /// <summary>Gets the value at the given index.</summary>
    /// <param name="index">The index</param>
    /// <returns>The value specified with the given index.</returns>
    object this[int index] { get; }

    /// <summary>Gets the value specified by the given key.</summary>
    /// <param name="key">The key</param>
    /// <returns>the value specified with the given key.</returns>
    object this[string key] { get; }

    /// <summary>Gets the key and value pairs in a <see cref="IReadOnlyDictionary{TKey,TValue}" />.</summary>
    IReadOnlyDictionary<string, object> Values { get; }

    /// <summary>Gets the keys in a <see cref="IReadOnlyList{T}" />.</summary>
    IReadOnlyList<string> Keys { get; }
}
