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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
                ? value is T castValue
                    ? castValue
                    : throw exceptionFact(
                        $"Expected key '{key}' to be of type '{typeof(T)}', but was '{value.GetType()}'.")
                : throw exceptionFact($"Expected key '{key}' to be present in the dictionary, but could not find.");
        }

        public T GetOptionalValue<T>(
            string key,
            T defaultValue,
            Func<string, Exception> exceptionFact)
        {
            return dict.TryGetValue<T>(key, out var value, exceptionFact) 
                ? value 
                : defaultValue;
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

        public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
        {
            return dict.TryGetValue<T>(key, out value, m => new InvalidOperationException(m));
        }

        public bool TryGetValue<T>(
            string key,
            [NotNullWhen(true)] out T? value,
            Func<string, Exception> exceptionFactory)
        {
            var found = dict.TryGetValue(key, out var uncastValue);

            if (found)
            {
                if (uncastValue is T goodValue)
                {
                    value = goodValue;
                    return true;
                }

                throw exceptionFactory(
                    $"Expected key '{key}' to be of type '{typeof(T)}', but was '{uncastValue!.GetType()}'.");
            }

            value = default;
            return false;
        }
    }
}
