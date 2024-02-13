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

internal class Record : IRecord
{
    private readonly IReadOnlyDictionary<string, int> _fieldLookup;
    private readonly IReadOnlyDictionary<string, int> _invariantFieldLookup;
    private readonly object[] _fieldValues;
    private IReadOnlyList<string> _keys;

    public Record(
        IReadOnlyDictionary<string, int> fieldLookup,
        IReadOnlyDictionary<string, int> invariantFieldLookup,
        object[] values)
    {
        _fieldLookup = fieldLookup;
        _invariantFieldLookup = invariantFieldLookup;
        _fieldValues = values;
    }

    /// <inheritdoc />
    public object this[int index] => _fieldValues[index];

    /// <inheritdoc cref="IRecord"/>
    public object this[string key] => _fieldValues[_fieldLookup[key]];

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        return _fieldValues[_fieldLookup[key]].As<T>();
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T value)
    {
        if (_fieldLookup.TryGetValue(key, out var index))
        {
            value = _fieldValues[index].As<T>();
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetCaseInsensitive<T>(string key)
    {
        return _fieldValues[_invariantFieldLookup[key]].As<T>();
    }

    /// <inheritdoc />
    public bool TryGetCaseInsensitive<T>(string key, out T value)
    {
        if (_invariantFieldLookup.TryGetValue(key, out var index))
        {
            value = _fieldValues[index].As<T>();
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetValueByCaseInsensitiveKey(string key, out object value)
    {
        if (_invariantFieldLookup.TryGetValue(key, out var index))
        {
            value = _fieldValues[index];
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Keys => _keys ??= _fieldLookup.Keys.ToList();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Values => this;

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _fieldLookup.ContainsKey(key);

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value)
    {
        var found = _fieldLookup.TryGetValue(key, out var index);
        value = found ? _fieldValues[index] : null;
        return found;
    }

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _fieldValues;

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return Keys.Select(key => new KeyValuePair<string, object>(key, this[key])).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();

    /// <inheritdoc />
    int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _fieldLookup.Count;
}
