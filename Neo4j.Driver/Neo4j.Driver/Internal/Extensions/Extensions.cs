// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal
{
    internal static class Extensions
    {
        public static T[] DequeueToArray<T>(this Queue<T> queue, int length)
        {
            var output = new T[length];
            for (var i = 0; i < length; i++)
            {
                output[i] = queue.Dequeue();
            }
            return output;
        }

        public static T GetMandatoryValue<T>(this IDictionary<string, object> dictionary, string key)
        {
            if(!dictionary.ContainsKey(key))
                throw new Neo4jException($"Required property '{key}' is not in the response.");

            return (T) dictionary[key];
        }

        public static T GetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue)
        {
            return dict.ContainsKey(key) ? (T) dict[key] : defaultValue;
        }

        public static string ToContentString<K, V>(this IDictionary<K, V> dict)
        {
            var output = dict.Select(item => $"{{{item.Key}, {item.Value}}}");
            return $"[{string.Join(", ", output)}]";
        }

        public static string ToContentString<K>(this IEnumerable<K> enumerable)
        {
            var output = enumerable.Select(item => $"{item}");
            return $"[{string.Join(", ", output)}]";
        }

    }
}