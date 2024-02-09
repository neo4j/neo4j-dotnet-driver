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

namespace Neo4j.Driver.Internal.Result;

internal class Record : IRecord
{
    private readonly Dictionary<string,object> _values;

    public Record(string[] keys, object[] values)
    {
        if (keys.Length != values.Length)
        {
            throw new ProtocolException(
                $"{nameof(keys)} length ({keys.Length}) does not equal to {nameof(values)} length ({values.Length})");
        }
        
        _values = new Dictionary<string, object>(keys.Length);

        for (var i = 0; i < keys.Length; i++)
        {
            _values.Add(keys[i], values[i]); 
        }

        Keys = keys;
    }

    public object this[int index] => Values[Keys[index]];
    public object this[string key] => Values[key];

    public IReadOnlyDictionary<string, object> Values => _values;
    public IReadOnlyList<string> Keys { get; }
}
