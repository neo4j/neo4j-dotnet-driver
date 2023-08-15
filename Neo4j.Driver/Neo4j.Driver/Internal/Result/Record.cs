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

namespace Neo4j.Driver.Internal.Result;

internal class Record : IRecord
{
    private readonly Dictionary<string, int> _fieldIndexes;
    private readonly object[] _values;

    public Record(string[] keys, Dictionary<string, int> fieldIndexes, object[] values)
    {
        if (keys.Length != values.Length)
        {
            throw new ProtocolException(
                $"{nameof(keys)} ({keys.Length.ToString()}) does not equal to {nameof(values)} ({values.Length.ToString()})");
        }
        
        _fieldIndexes = fieldIndexes;
        _values = values;
        Keys = keys;
    }

    public object this[int index] => _values[index];
    public object this[string key] => _values[_fieldIndexes[key]];

    public IReadOnlyDictionary<string, object> Values
    {
        get
        {
            var valueKeys = new Dictionary<string, object>(_values.Length);
            for (var i = 0; i < _values.Length; i++)
            {
                valueKeys.Add(Keys[i], _values[i]);
            }

            return valueKeys;
        }
    }

    public IReadOnlyList<string> Keys { get; }
}
