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

namespace Neo4j.Driver.Internal.Util;

internal readonly struct DictionaryAccessWrapper(IDictionary dictionary) : IDictionary<string, object>
{
    public object this[string key]
    {
        get => dictionary[key];
        set => throw new NotSupportedException("This dictionary is read-only.");
    }

    public ICollection<string> Keys => dictionary.Keys.Cast<string>().ToList();
    public ICollection<object> Values => dictionary.Values.Cast<object>().ToList();

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
    {
        throw new NotSupportedException("This dictionary is read-only.");
    }

    public int Count => dictionary.Count;
    public bool IsReadOnly => true;

    public void Add(string key, object value) => throw new NotSupportedException("This dictionary is read-only.");

    public bool ContainsKey(string key)
    {
        return dictionary.Contains(key);
    }

    public bool Remove(string key) => throw new NotSupportedException("This dictionary is read-only.");

    public bool TryGetValue(string key, out object value)
    {
        if (dictionary.Contains(key))
        {
            value = dictionary[key];
            return true;
        }

        value = null;
        return false;
    }

    public void Add(KeyValuePair<string, object> item) =>
        throw new NotSupportedException("This dictionary is read-only.");

    public void Clear() => throw new NotSupportedException("This dictionary is read-only.");

    public bool Contains(KeyValuePair<string, object> item)
    {
        return TryGetValue(item.Key, out var value) && Equals(value, item.Value);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotSupportedException();

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        foreach (DictionaryEntry entry in dictionary)
        {
            yield return new KeyValuePair<string, object>((string)entry.Key, entry.Value);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
    }
}
