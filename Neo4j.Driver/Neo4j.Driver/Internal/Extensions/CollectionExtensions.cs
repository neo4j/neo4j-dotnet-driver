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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal static class CollectionExtensions
{
    private const string DefaultItemSeparator = ", ";
    private static readonly TypeInfo NeoValueTypeInfo = typeof(IValue).GetTypeInfo();
    
    private static readonly IParameterValueTransformer _parameterValueTransformer =
        new ParameterValueTransformer();
    
    private static readonly IObjectToDictionaryConverter _objectToParameterDictionaryConverter = 
        new ObjectToParameterDictionaryConverter();

    extension(object obj)
    {
        public string ToContentString(string separator = DefaultItemSeparator)
        {
            return obj switch
            {
                null => "NULL",
                string => obj.ToString(),
                IDictionary dictionary => dictionary.ToContentString(separator),
                IEnumerable enumerable => enumerable.ToContentString(separator),
                _ => obj.ToString()
            };
        }

        public IDictionary<string, object> ToParameterDictionary()
        {
            return _objectToParameterDictionaryConverter.Convert(obj);
        }
    }

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
            foreach (var key in other.Keys)
            {
                if (!dict.ContainsKey(key))
                {
                    dict[key] = other[key];
                }
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
    }

    extension(IDictionary dict)
    {
        private string ToContentString(string separator)
        {
            var dictStrings = dict.Keys.Cast<object>()
                .Select(key => $"{{{key.ToContentString()}, {dict[key].ToContentString()}}}");

            return $"[{string.Join(separator, dictStrings)}]";
        }
    }

    extension(IEnumerable enumerable)
    {
        private string ToContentString(string separator)
        {
            var listStrings = from object item in enumerable select item.ToContentString();
            return $"[{string.Join(separator, listStrings)}]";
        }
    }
}
