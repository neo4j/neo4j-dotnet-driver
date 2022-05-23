// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;

namespace Neo4j.Driver.Internal.Serialization;

internal class PropsAndFieldConverterFactory
{
    public Action<IReadOnlyDictionary<string, object>, object> GenerateForField(FieldInfo fieldInfo)
    {
        if (fieldInfo.IsInitOnly)
            return null;

        var customAttributes = fieldInfo.GetCustomAttributes(typeof(BaseNeo4jPropertyAttribute), true);

        if (customAttributes.Length == 0)
            return GenerateDefaultReader(fieldInfo, null);

        return customAttributes[0] switch
        {
            Neo4jPropertyAttribute propertyAttribute => GenerateDefaultReader(fieldInfo, propertyAttribute),
            Neo4jIgnoreAttribute => null,
            _ => throw new Exception()
        };
    }

    public Action<IReadOnlyDictionary<string, object>, object> GenerateForProperties(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanWrite)
            return null; 

        var customAttributes = propertyInfo.GetCustomAttributes(typeof(BaseNeo4jPropertyAttribute), true);

        if (customAttributes.Length == 0)
            return GenerateDefaultReader(propertyInfo, null);

        return customAttributes[0] switch
        {
            Neo4jPropertyAttribute propertyAttribute => GenerateDefaultReader(propertyInfo, propertyAttribute),
            Neo4jIgnoreAttribute => null,
            _ => throw new Exception()
        };
    }

    private Action<IReadOnlyDictionary<string, object>, object> GenerateDefaultReader(FieldInfo fieldInfo, Neo4jPropertyAttribute attribute)
    {
        if (attribute != null)
        {
            return (dictionary, instance) =>
            {
                if (!dictionary.TryGetValue(attribute.Name, out var data))
                {
                    if (attribute.AllowNull)
                        return;

                    throw new SerializationException();
                }
                 
                fieldInfo.SetValue(instance, CoerceType(fieldInfo.FieldType, data));
            };
        }

        return (dictionary, instance) =>
        {
            var stringName = fieldInfo.Name;
            if (!dictionary.TryGetValue(stringName, out var data))
            {
                var charArray = stringName.ToCharArray();
                charArray[0] = char.ToLower(charArray[0]);
                if (!dictionary.TryGetValue(charArray.ToString(), out data))
                    return;
            }

            fieldInfo.SetValue(instance, CoerceType(fieldInfo.FieldType, data));
        };
    }

    private Action<IReadOnlyDictionary<string, object>, object> GenerateDefaultReader(PropertyInfo fieldInfo, Neo4jPropertyAttribute attribute)
    {
        if (attribute != null)
        {
            return (dictionary, instance) =>
            {
                if (!dictionary.TryGetValue(attribute.Name, out var data))
                {
                    if (attribute.AllowNull)
                        return;

                    throw new SerializationException();
                }

                fieldInfo.SetValue(instance, CoerceType(fieldInfo.PropertyType, data));
            };
        }

        return (dictionary, instance) =>
        {
            var stringName = fieldInfo.Name;
            if (!dictionary.TryGetValue(stringName, out var data))
            {
                var charArray = stringName.ToCharArray();
                charArray[0] = char.ToLower(charArray[0]);
                if (!dictionary.TryGetValue(charArray.ToString(), out data))
                    return;
            }

            fieldInfo.SetValue(instance, CoerceType(fieldInfo.PropertyType, data));
        };
    }

    private static object CoerceType(Type type, object data)
    {
        var dataType = data.GetType();

        if (dataType == type)
            return data;

        switch (data)
        {
            case double or long:
            {
                var typeCode = Type.GetTypeCode(type);
                return Convert.ChangeType(data, typeCode);
            }
            case Dictionary<string, object> dictionary:
                return Neo4jSerialization.GetOrGenerateConverter(dataType).Deserialize(dictionary, type);
            case IList dataCollection:
            {
                if (type.IsInterface)
                    throw new Exception($"Could not create instance of {type}.");

                var collection = Activator.CreateInstance(type, dataCollection.Count) as IList;
                var innerType = type.GenericTypeArguments[0];
                    
                for (var i = 0; i < dataCollection.Count; i++)
                {
                    collection[i] = CoerceType(innerType, dataCollection[i]);
                }

                return collection;
            }
            case INode node:
                break;
            case IRelationship relationship:
                break;
            default:
                throw new Exception();
        }
    }
}