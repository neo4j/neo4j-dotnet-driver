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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal;

internal static class StringObjectDictionaryExtensions
{
    extension(IDictionary<string, object> dict)
    {
        public T GetMandatoryValue<T>(
            string key,
            Func<string, Exception> exceptionFact)
        {
            return dict.TryGetValue(key, out var value)
                ? (T)value
                : throw exceptionFact($"Expected key '{key}' to be present in the dictionary, but could not find.");
        }
    
        public T GetValue<T>(string key, T defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }

        public bool TryGetValue<T>(string key, T defaultValue, out T value)
        {
            if (dict.TryGetValue(key, out var uncastValue))
            {
                value = (T)uncastValue;
                return true;
            }

            value = defaultValue;
            return false;
        }
    }
}

