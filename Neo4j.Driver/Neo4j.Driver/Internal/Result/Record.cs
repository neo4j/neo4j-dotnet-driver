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
using System.Linq;

namespace Neo4j.Driver.Internal.Result;

internal class Record : IRecord
{
    private readonly IReadOnlyDictionary<string, int> _fieldLookup;
    private readonly IReadOnlyDictionary<string, int> _invariantFieldLookup;
    private readonly object[] _fieldValues;
    private IndirectDictionary<string, object> _valuesDictionary;

    public Record(
        IReadOnlyDictionary<string, int> fieldLookup,
        IReadOnlyDictionary<string, int> invariantFieldLookup,
        object[] values)
    {
        _fieldLookup = fieldLookup;
        _invariantFieldLookup = invariantFieldLookup;
        _fieldValues = values;
        Keys = new ArrayToReadOnlyListWrapper<string>(_fieldLookup.Keys.ToArray());
    }

    /// <inheritdoc />
    public object this[int index] => _fieldValues[index];

    /// <inheritdoc />
    public object this[string key] => _fieldValues[_fieldLookup[key]];

    /// <inheritdoc />
    public object GetValueByCaseInsensitiveKey(string key)
    {
        return _fieldValues[_invariantFieldLookup[key]];
    }

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
    public IReadOnlyDictionary<string, object> Values =>
        _valuesDictionary ??= new IndirectDictionary<string, object>(_fieldLookup, _fieldValues);

    /// <inheritdoc />
    public IReadOnlyList<string> Keys { get; }
}
