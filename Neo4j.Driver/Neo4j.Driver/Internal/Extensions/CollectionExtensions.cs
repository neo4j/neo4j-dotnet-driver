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

internal static partial class CollectionExtensions
{
    private const string DefaultItemSeparator = ", ";
    private static readonly TypeInfo NeoValueTypeInfo = typeof(IValue).GetTypeInfo();
    
    private static readonly IParameterValueTransformer _parameterValueTransformer =
        new ParameterValueTransformer();
    
    private static readonly IObjectToDictionaryConverter _objectToDictionaryConverter = 
        new ObjectToDictionaryConverter();

    public static T GetMandatoryValue<T>(
        this IDictionary<string, object> dictionary,
        string key,
        Func<string, Exception> exceptionFact)
    {
        if (!dictionary.ContainsKey(key))
        {
            throw exceptionFact($"Expected key '{key}' to be present in the dictionary, but could not find.");
        }

        return (T)dictionary[key];
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        TKey key,
        TValue defaultValue = default)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public static T GetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue)
    {
        return dict.TryGetValue(key, out var value) ? (T)value : defaultValue;
    }

    public static bool TryGetValue<T>(this IDictionary<string, object> dict, string key, T defaultValue, out T value)
    {
        if (dict.TryGetValue(key, out var uncastValue))
        {
            value = (T)uncastValue;
            return true;
        }

        value = defaultValue;
        return false;
    }

    private static string ToContentString(this IDictionary dict, string separator)
    {
        var dictStrings = dict.Keys.Cast<object>()
            .Select(key => $"{{{key.ToContentString()}, {dict[key].ToContentString()}}}");

        return $"[{string.Join(separator, dictStrings)}]";
    }

    private static string ToContentString(this IEnumerable enumerable, string separator)
    {
        var listStrings = from object item in enumerable select item.ToContentString();
        return $"[{string.Join(separator, listStrings)}]";
    }

    public static string ToContentString(this object o, string separator = DefaultItemSeparator)
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
            return ToContentString((IDictionary)o, separator);
        }

        if (o is IEnumerable)
        {
            return ToContentString((IEnumerable)o, separator);
        }

        return o.ToString();
    }

    public static IDictionary<string, object> ToDictionary(this object o)
    {
        return _objectToDictionaryConverter.Convert(o);
    }

    private static IDictionary<string, object> FillDictionary(object o, IDictionary<string, object> dict)
    {
        foreach (var propInfo in o.GetType().GetRuntimeProperties())
        {
            var name = propInfo.Name;
            var value = propInfo.GetValue(o);
            var valueTransformed = _parameterValueTransformer.Transform(value);

            dict.Add(name, valueTransformed);
        }

        return dict;
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

    public static void FillMissingFrom<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
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

    public static void OverwriteFrom<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        params (TKey key, TValue value)[] pairs)
    {
        OverwriteFrom(dict, default, pairs);
    }

    public static void OverwriteFrom<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
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
