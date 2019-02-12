// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal
{
    internal static class CollectionExtensions
    {
        private static readonly TypeInfo NeoValueTypeInfo = typeof(IValue).GetTypeInfo();
        private const string DefalutItemSeparator = ", ";

        public static T GetMandatoryValue<T>(this IDictionary<string, object> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
                throw new Neo4jException($"Required property '{key}' is not in the response.");

            return (T) dictionary[key];
        }

        public static T GetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue)
        {
            return dict.ContainsKey(key) ? (T) dict[key] : defaultValue;
        }

        private static string ToContentString(this IDictionary dict, string separator)
        {
            var dictStrings = from object key in dict.Keys
                select $"{{{key.ToContentString()}, {dict[key].ToContentString()}}}";
            return $"[{string.Join(separator, dictStrings)}]";
        }

        private static string ToContentString(this IEnumerable enumerable, string separator)
        {
            var listStrings = from object item in enumerable select item.ToContentString();
            return $"[{string.Join(separator, listStrings)}]";
        }

        public static string ToContentString(this object o, string separator = DefalutItemSeparator)
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
                return ToContentString((IDictionary) o, separator);
            }

            if (o is IEnumerable)
            {
                return ToContentString((IEnumerable) o, separator);
            }

            return o.ToString();
        }

        public static IDictionary<string, object> ToDictionary(this object o)
        {
            if (o == null)
            {
                return null;
            }

            if (o is Dictionary<string, object> dict)
            {
                return dict;
            }

            if (o is IDictionary<string, object> dictInt)
            {
                return new Dictionary<string, object>(dictInt);
            }

            if (o is IReadOnlyDictionary<string, object> dictIntRo)
            {
                return dictIntRo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
                    var convertedList = new List<object>(((IList) value).Count);
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
                var elementType = (Type) null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[0];
                }

                if (elementType == null || elementType.NeedsConversion())
                {
                    var convertedList = new List<object>(((IList) value).Count);
                    foreach (var element in (IEnumerable) value)
                    {
                        convertedList.Add(Transform(element));
                    }

                    value = convertedList;
                }
            }
            else if (value is IDictionary)
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type) null;

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

                        convertedDict.Add((string) key, Transform(dict[key]));
                    }

                    value = convertedDict;
                }
            }
            else if (value is IEnumerable && !(value is string))
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type) null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[0];
                }

                if (elementType == null || elementType.NeedsConversion())
                {
                    var convertedList = new List<object>();
                    foreach (var element in (IEnumerable) value)
                    {
                        convertedList.Add(Transform(element));
                    }

                    value = convertedList;
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

            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsValueType)
            {
                return false;
            }

            if (NeoValueTypeInfo.IsAssignableFrom(typeInfo))
            {
                return false;
            }

            return true;
        }
    }
}