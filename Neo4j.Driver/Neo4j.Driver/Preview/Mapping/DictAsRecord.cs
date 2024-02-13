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

namespace Neo4j.Driver.Preview.Mapping;

internal class DictAsRecord : IRecord
{
    private readonly IReadOnlyDictionary<string, object> _dict;

    public DictAsRecord(object dict, IRecord record)
    {
        var readOnlyDictionary = dict switch
        {
            IEntity entity => entity.Properties ?? new Dictionary<string, object>(),
            IReadOnlyDictionary<string, object> dictionary => dictionary,
            _ => throw new InvalidOperationException($"Cannot create a DictAsRecord from a {dict.GetType().Name}")
        };

        // this is only used by the default mapper so make it case insensitive
        _dict = readOnlyDictionary.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);

        Record = record;
    }

    public IRecord Record { get; }

    public object this[int index] => _dict.TryGetValue(_dict.Keys.ElementAt(index), out var obj) ? obj : null;
    public object this[string key] => _dict.TryGetValue(key, out var obj) ? obj : null;

    public bool TryGetValueByCaseInsensitiveKey(string key, out object value)
    {
        return _dict.TryGetValue(key, out value);
    }

    public IReadOnlyDictionary<string, object> Values => _dict;
    public IReadOnlyList<string> Keys => _dict.Keys.ToList();

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _dict.ContainsKey(key);

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) =>
        _dict.TryGetValue(key, out value);

    /// <inheritdoc />
    int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _dict.Count;

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _dict.Keys;

    /// <inheritdoc />
    IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _dict.Values;
}
