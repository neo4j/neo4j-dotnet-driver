// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Preview.Mapping;

internal static class DefaultMapper
{
    private static readonly Dictionary<Type, IRecordMapper> Mappers = new();

    public static IRecordMapper<T> Get<T>() where T : new()
    {
        var type = typeof(T);
        if (Mappers.TryGetValue(type, out var mapper))
        {
            return (IRecordMapper<T>)mapper;
        }

        var mappingBuilder = new MappingBuilder<T>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var setter = property.GetSetMethod();
            if (setter is null)
            {
                continue;
            }

            string path = property.Name;
            var mappingPathAttribute = property.GetCustomAttribute<MappingPathAttribute>();
            if (mappingPathAttribute is not null)
            {
                path = mappingPathAttribute.Path;
            }

            mappingBuilder.Map(setter, path.ToLower());
        }

        mapper = mappingBuilder.Build();
        Mappers[type] = mapper;
        return (IRecordMapper<T>)mapper;
    }
}
