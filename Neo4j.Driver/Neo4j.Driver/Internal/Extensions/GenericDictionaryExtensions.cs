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

namespace Neo4j.Driver.Internal;

internal static class GenericDictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dict)
    {
        public TValue GetValueOrDefault(
            TKey key,
            TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void FillMissingFrom(
            IDictionary<TKey, TValue> other)
        {
            var missing = other
                .Keys
                .Where(key => !dict.ContainsKey(key));
            
            foreach (var key in missing)
            {
                dict[key] = other[key];
            }
        }

        public void OverwriteFrom(params (TKey key, TValue value)[] pairs)
        {
            dict.OverwriteFrom(default, pairs);
        }

        public void OverwriteFrom(
            TValue ignoreValue,
            params (TKey key, TValue value)[] pairs)
        {
            foreach (var (key, value) in pairs)
            {
                if (!Equals(value, ignoreValue))
                {
                    dict[key] = value;
                }
            }
        }

        public string ToContentString(string separator = ", ")
        {
            var dictStrings = dict.Select(kvp =>
                $"{{{kvp.Key.ToContentString()}, {kvp.Value.ToContentString()}}}");

            return $"[{string.Join(separator, dictStrings)}]";
        }
    }
}
