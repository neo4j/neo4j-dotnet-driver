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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Result;

internal class IndirectDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly IReadOnlyDictionary<TKey, int> _lookup;
    private readonly TValue[] _values;

    public IndirectDictionary(IReadOnlyDictionary<TKey, int> lookup, TValue[] values)
    {
        _lookup = lookup;
        _values = values;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _lookup.Select(pair => new KeyValuePair<TKey, TValue>(pair.Key, _values[pair.Value])).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _lookup.Count;

    public bool ContainsKey(TKey key)
    {
        return _lookup.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_lookup.TryGetValue(key, out var index))
        {
            value = _values[index];
            return true;
        }

        value = default;
        return false;
    }

    public TValue this[TKey key] => _values[_lookup[key]];

    public IEnumerable<TKey> Keys => _lookup.Keys;

    public IEnumerable<TValue> Values => _values;
}
