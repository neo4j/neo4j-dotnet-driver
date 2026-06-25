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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Mapping;

internal static class DefaultMapper
{
    private static readonly ConcurrentDictionary<Type, object> Mappers = new();
    private static readonly IMappingBindingProvider MappingBindingProvider = new MappingBindingProvider();

    public static void Reset()
    {
        Mappers.Clear();
    }

    public static IRecordMapper<T> Get<T>(HashSet<MethodInfo> mappedSetters = null)
    {
        mappedSetters ??= [];
        var type = typeof(T);

        var result = Mappers.GetOrAdd(type, _ => BuildDefaultMapper<T>(mappedSetters, type));
        return (IRecordMapper<T>)result;
    }
    
    private static IRecordMapper<T> BuildDefaultMapper<T>(IReadOnlySet<MethodInfo> mappedSetters, Type type)
    {
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
            // only public setters are mapped. Also ignore properties with MappingIgnoredAttribute
            // or whose setter has already been mapped elsewhere (e.g. custom mapping config).
            var setter = property.GetSetMethod();
            if (setter is null ||
                property.GetCustomAttribute<MappingIgnoredAttribute>() is not null ||
                mappedSetters.Contains(setter))
            {
                continue;
            }

            var mappingBinding = MappingBindingProvider.GetMappingBinding(property);

            // don't re-map any fields that were already mapped by the constructor
            if (!usedEntitySources.Contains(ResolveRecordField(mappingBinding.Path, mappingBinding.Explicit)))
            {
                mappingBuilder.Map(setter, mappingBinding);
            }
        }

        return mappingBuilder.Build();
    }

    private static HashSet<string> GetUsedEntitySources<T>(ConstructorInfo constructor)
    {
        var isRecordType = IsRecord(typeof(T));
        var usedEntitySources = new HashSet<string>();

        foreach (var parameter in constructor.GetParameters())
        {
            var mappingBinding = MappingBindingProvider.GetMappingBinding(parameter);

            // for record types the positional parameter and its generated property share a name, and the property
            // is mapped by that name rather than by any [MappingSource] on the parameter, so key on the name.
            var (path, isExplicit) = isRecordType
                ? (parameter.Name, false)
                : (mappingBinding.Path, mappingBinding.Explicit);

            usedEntitySources.Add(ResolveRecordField(path, isExplicit));
        }

        return usedEntitySources;
    }

    // resolves a binding's path to the record field it actually reads, so that the used-source comparison matches
    // on the database field rather than the raw C# name. Non-explicit members are subject to identifier
    // translation (e.g. a ctor param 'yearsOfService' and a property 'YearsOfService' both resolve to the same
    // field), while members with [MappingSource] use their path verbatim.
    private static string ResolveRecordField(string path, bool isExplicit)
    {
        return isExplicit ? path : RecordObjectMapping.Instance.GetTranslatedRecordIdentifier(path);
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
