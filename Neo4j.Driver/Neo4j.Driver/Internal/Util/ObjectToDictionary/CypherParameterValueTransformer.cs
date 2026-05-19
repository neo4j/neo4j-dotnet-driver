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

namespace Neo4j.Driver.Internal.Util;

internal class CypherParameterValueTransformer : ICypherParameterValueTransformer
{
    private static readonly TypeInfo NeoValueTypeInfo = typeof(IValue).GetTypeInfo();

    public object Transform(object value, Func<object, IDictionary<string, object>, IDictionary<string, object>> fillDictionary)
    {
        fillDictionary = fillDictionary ?? throw new ArgumentNullException(nameof(fillDictionary));
        
        var valueType = value?.GetType();
        if (valueType == null || !NeedsConversion(valueType))
        {
            return value;
        }

        switch (value)
        {
            case Array array:
            {
                var elementType = valueType.GetElementType();

                if (NeedsConversion(elementType))
                {
                    // recursive call
                    value = array.Cast<object>().Select(x => Transform(x, fillDictionary)).ToList();
                }

                break;
            }

            case IList list:
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                Type elementType = null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[0];
                }

                if (elementType == null || NeedsConversion(elementType))
                {
                    var convertedList = new List<object>(list.Count);
                    foreach (var element in list)
                    {
                        convertedList.Add(Transform(element, fillDictionary));
                    }

                    value = convertedList;
                }

                break;
            }

            case IDictionary dictionary:
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type)null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[1];
                }

                if (elementType == null || NeedsConversion(elementType))
                {
                    var dict = dictionary;

                    var convertedDict = new Dictionary<string, object>(dict.Count);
                    foreach (var key in dict.Keys)
                    {
                        if (key is not string str)
                        {
                            throw new InvalidOperationException(
                                "dictionaries passed as part of a parameter to cypher queries should have string keys!");
                        }

                        convertedDict.Add(str, Transform(dict[str], fillDictionary));
                    }

                    value = convertedDict;
                }

                break;
            }

            case IEnumerable enumerable and not string:
            {
                var valueTypeInfo = valueType.GetTypeInfo();
                var elementType = (Type)null;

                if (valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    elementType = valueTypeInfo.GenericTypeArguments[0];
                }

                if (elementType == null || NeedsConversion(elementType))
                {
                    var converted = enumerable.Cast<object>().Select(x => Transform(x, fillDictionary));
                    value = new List<object>(converted);
                }
                break;
            }
            
            default:
            {
                if (NeedsConversion(valueType))
                {
                    value = fillDictionary(value, new Dictionary<string, object>());
                }

                break;
            }
        }

        return value;
    }

    private bool NeedsConversion(Type type)
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
