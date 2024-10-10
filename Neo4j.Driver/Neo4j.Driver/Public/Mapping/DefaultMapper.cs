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
using System.Reflection;

namespace Neo4j.Driver.Mapping;

internal static class DefaultMapper
{
    private static readonly Dictionary<Type, object> Mappers = new();

    public static void Reset()
    {
        Mappers.Clear();
    }

    public static IRecordMapper<T> Get<T>(HashSet<MethodInfo> mappedSetters = null)
    {
        mappedSetters ??= [];
        var type = typeof(T);

        // if we already have a mapper for this type, return it
        if (Mappers.TryGetValue(type, out var mapper))
        {
            return (IRecordMapper<T>)mapper;
        }

        // decide which constructor we're going to use
        var mappingBuilder = new MappingBuilder<T>();
        var constructor = GetCorrectConstructor<T>();
        mappingBuilder.UseConstructor(constructor);

        // keep a list of the entity sources that are used by the constructor, so we don't re-map them later
        var usedEntitySources = GetUsedEntitySources<T>(constructor);

        // after the constructor is used to create the object, map any remaining properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            // ignore properties without a setter or with MappingIgnoredAttribute, or compiler generated,
            // or if the setter has already been mapped elsewhere (e.g. custom mapping config)
            if (property.SetMethod is null ||
                property.GetCustomAttribute<MappingIgnoredAttribute>() is not null ||
                mappedSetters.Contains(property.SetMethod))
            {
                continue;
            }

            // check if there is a MappingSourceAttribute: if there is, use the specified mapping source;
            // if not, look for a property on the entity with the same name as the property on the object
            var mappingSource = property.GetEntityMappingInfo();

            // don't re-map any fields that were already mapped by the constructor
            if (!usedEntitySources.Contains(mappingSource.Path))
            {
                mappingBuilder.Map(property.SetMethod, mappingSource);
            }
        }

        mapper = mappingBuilder.Build();

        // cache the mapper for future use
        Mappers[type] = mapper;
        return (IRecordMapper<T>)mapper;
    }

    private static HashSet<string> GetUsedEntitySources<T>(ConstructorInfo constructor)
    {
        var isRecordType = IsRecord(typeof(T));
        var usedEntitySources = new HashSet<string>();

        foreach (var parameter in constructor.GetParameters())
        {
            var key = parameter.GetCustomAttribute<MappingSourceAttribute>()?.EntityMappingInfo?.Path;
            if (key == null || isRecordType)
            {
                key = parameter.Name;
            }

            usedEntitySources.Add(key);
        }

        return usedEntitySources;
    }

    private static bool IsRecord(Type type)
    {
        return type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) != null;
    }

    private static ConstructorInfo GetCorrectConstructor<T>()
    {
        // get all the constructors in the type
        var constructors = typeof(T).GetConstructors();

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"Cannot map to type {typeof(T).Name} because it does not have any constructors.");
        }

        var fewestParamCount = int.MaxValue;
        ConstructorInfo fewestParamConstructor = null!;
        foreach (var constructor in constructors)
        {
            // if one of them has the MappingConstructor attribute, return that one
            if (constructor.GetCustomAttribute<MappingConstructorAttribute>() is not null)
            {
                return constructor;
            }

            var paramCount = constructor.GetParameters().Length;
            if (paramCount < fewestParamCount)
            {
                fewestParamCount = paramCount;
                fewestParamConstructor = constructor;
            }
        }

        // otherwise return the constructor with the fewest parameters
        return fewestParamConstructor;
    }
}
