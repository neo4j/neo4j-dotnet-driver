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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Util;

internal class MultiMap<TKey, TValue> 
{
    private readonly ConcurrentDictionary<TKey, IList<TValue>> _store = new();

    public IList<TValue> this[TKey key]
    {
        get
        {
            return _store.GetOrAdd(key, _ => new List<TValue>());
        }
    }

    public IEnumerable<TValue> GetEnumerable(TKey key)
    {
        return _store.TryGetValue(key, out var list) ? list : Enumerable.Empty<TValue>();
    }
}
