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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Mapping;

internal sealed class DictAsRecord : IRecord
{
    private readonly IReadOnlyDictionary<string, object> _caseSensitiveDict;
    private readonly IReadOnlyDictionary<string, object> _caseInsensitiveDict;

    public DictAsRecord(object dict, IRecord record)
    {
        var readOnlyDictionary = dict switch
        {
            IEntity entity => entity.Properties ?? new Dictionary<string, object>(),
            IReadOnlyDictionary<string, object> dictionary => dictionary,
            _ => throw new InvalidOperationException($"Cannot create a DictAsRecord from a {dict.GetType().Name}")
        };

        // fill the case-sensitive dictionary
        _caseSensitiveDict = readOnlyDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);

        _caseInsensitiveDict = readOnlyDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.InvariantCultureIgnoreCase);

        Record = record;
    }

    public IRecord Record { get; }

    public object this[int index] => _caseSensitiveDict.TryGetValue(_caseInsensitiveDict.Keys.ElementAt(index), out var obj) ? obj : null;
    public object this[string key] => _caseSensitiveDict.TryGetValue(key, out var obj) ? obj : null;

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        return _caseSensitiveDict[key].As<T>();
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T value)
    {
        if (_caseSensitiveDict.TryGetValue(key, out var obj))
        {
            value = obj.As<T>();
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public T GetCaseInsensitive<T>(string key)
    {
        return _caseInsensitiveDict[key].As<T>();
    }

    /// <inheritdoc />
    public bool TryGetCaseInsensitive<T>(string key, out T value)
    {
        if (_caseInsensitiveDict.TryGetValue(key, out var obj))
        {
            value = obj.As<T>();
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetValueByCaseInsensitiveKey(string key, out object value)
    {
        return _caseInsensitiveDict.TryGetValue(key, out value);
    }

    public IReadOnlyDictionary<string, object> Values => _caseSensitiveDict;
    public IReadOnlyList<string> Keys => _caseSensitiveDict.Keys.ToList();

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return _caseSensitiveDict.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _caseSensitiveDict.GetEnumerator();
    }

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _caseSensitiveDict.ContainsKey(key);

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) =>
        _caseSensitiveDict.TryGetValue(key, out value);

    /// <inheritdoc />
    int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _caseSensitiveDict.Count;

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _caseSensitiveDict.Keys;

    /// <inheritdoc />
    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _caseSensitiveDict.Values;
}
