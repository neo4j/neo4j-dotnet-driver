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

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo4j.Driver.Internal.QueryApi;

internal sealed class QueryApiParameterDictionary(IDictionary<string, object> parameters)
    : IReadOnlyDictionary<string, object>
{
    public int Count => parameters.Count;
    public IEnumerable<string> Keys => parameters.Keys;
    public IEnumerable<object> Values => parameters.Values;
    public object this[string key] => parameters[key];
    public bool ContainsKey(string key) => parameters.ContainsKey(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => parameters.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => parameters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
