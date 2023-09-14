﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Threading.Tasks;

namespace Neo4j.Driver.Preview.Mapping;

/// <summary>
/// Contains extension methods for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Materialises the <see cref="IAsyncEnumerable{T}"/> into a list of objects of type
    /// <typeparamref name="T"/>, by mapping each record in the enumerable to an object.
    /// If no custom mapper is defined for type <typeparamref name="T"/>, the default
    /// mapper will be used.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <param name="asyncEnumerable">The asynchronous source of records.</param>
    /// <typeparam name="T">The type of object to map to.</typeparam>
    /// <returns>The list of mapped objects.</returns>
    public static async Task<IReadOnlyList<T>> ToObjectListAsync<T>(this IResultCursor asyncEnumerable)
        where T : new()
    {
        var list = new List<T>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false))
        {
            list.Add(item.AsObject<T>());
        }

        return list;
    }
}
