// Copyright (c) "Neo4j"
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

public static class IAsyncEnumerableExtensions
{
    public static async Task<IEnumerable<T>> ToObjectListAsync<T>(this IAsyncEnumerable<IRecord> asyncEnumerable)
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
