// Copyright (c) 2002-2017 "Neo Technology,"
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal static class CollectionExtensions
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
            if (!dictionary.ContainsKey(key))
                throw new Neo4jException($"Required property '{key}' is not in the response.");

            return (T)dictionary[key];
        }

        public static T GetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue)
        {
            return dict.ContainsKey(key) ? (T)dict[key] : defaultValue;
        }

        public static string ToContentString<K, V>(this IDictionary<K, V> dict)
        {
            var output = dict.Select(item => $"{{{item.Key}, {item.Value.ValueToString()}}}");
            return $"[{string.Join(", ", output)}]";
        }

        public static string ToContentString<K>(this IEnumerable<K> enumerable, string separator = ", ")
        {
            var output = enumerable.Select(item => $"{item}");
            return $"[{string.Join(separator, output)}]";
        }

        public static string ValueToString(this object o)
        {
            if (o == null)
            {
                return "NULL";
            }
            if (o is string)
            {
                return o.ToString();
            }
            if (o is IDictionary)
            {
                var dict = (IDictionary) o;
                var dictStrings = (from object key in dict.Keys select $"{{{key.ValueToString()} : {dict[key].ValueToString()}}}").ToList();
                return $"[{string.Join(", ", dictStrings)}]";
            }
            if (o is IEnumerable)
            {
                var listStrings = (from object item in ((IEnumerable) o) select item.ValueToString());
                return $"[{string.Join(", ", listStrings)}]";
            }

            return o.ToString();
        }

        public static IDictionary<string, object> ToDictionary(this object o)
        {
            if (o == null)
            {
                return null;
            }

            return FillDictionary(o, new Dictionary<string, object>());
        }

        private static IDictionary<string, object> FillDictionary(object o, IDictionary<string, object> dict)
        {
            foreach (var propInfo in o.GetType().GetRuntimeProperties())
            {
                var name = propInfo.Name;
                var value = propInfo.GetValue(o);
                var valueTransformed = Transform(value);

                dict.Add(name, valueTransformed);
            }

            return dict;
        }

        private static object Transform(object value)
        {
            if (value == null)
            {
                return null;
            }

            var valueType = value.GetType();

            if (value is Array)
            {
                var elementType = valueType.GetElementType();

                if (elementType.NeedsConversion())
                {
                    var convertedList = new List<object>(((IList)value).Count);
                    foreach (var element in (IEnumerable) value)
                    {
                        convertedList.Add(Transform(element));
                    }
                    value = convertedList;
                }
            }
            else if (value is IList)
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type)null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[0];
                }

                if (elementType == null || elementType.NeedsConversion())
                {
                    var convertedList = new List<object>(((IList)value).Count);
                    foreach (var element in (IEnumerable)value)
                    {
                        convertedList.Add(Transform(element));
                    }
                    value = convertedList;
                }
            }
            else if (value is IDictionary)
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type)null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[1];
                }

                if (elementType == null || elementType.NeedsConversion())
                {
                    var dict = (IDictionary) value;

                    var convertedDict = new Dictionary<string, object>(dict.Count);
                    foreach (object key in dict.Keys)
                    {
                        if (!(key is string))
                        {
                            throw new InvalidOperationException(
                                "dictionaries passed as part of a parameter to cypher statements should have string keys!");
                        }

                        convertedDict.Add((string)key, Transform(dict[key]));
                    }
                    value = convertedDict;
                }
            }
            else
            {
                if (valueType.NeedsConversion())
                {
                    value = FillDictionary(value, new Dictionary<string, object>());
                }
            }

            return value;
        }

        private static bool NeedsConversion(this Type type)
        {
            if (type == typeof(string))
            {
                return false;
            }

            if (type.GetTypeInfo().IsValueType)
            {
                return false;
            }

            return true;
        }

    }
}
