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
using System.Linq;

namespace Neo4j.Driver.Preview.Mapping;

public interface IMappingRegistry
{
    IMappingRegistry RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder) where T : new();
}

public class RecordObjectMapping : IMappingRegistry
{
    private static object _lockObject = new();
    private static RecordObjectMapping Instance = new();

    private readonly Dictionary<Type, IRecordMapper> _mappers = new();

    internal static void Reset()
    {
        lock (_lockObject)
        {
            Instance = new RecordObjectMapping();
        }
    }

    public static void Register(IRecordMapper mapper)
    {
        lock (_lockObject)
        {
            if (!IsValidMapper(mapper.GetType(), out var genericInterface))
            {
                throw new ArgumentException("Mapper type must implement IRecordMapper<>");
            }

            var destinationType = genericInterface.GetGenericArguments()[0];
            Instance._mappers[destinationType] = mapper;
        }
    }

    private static bool IsValidMapper(Type type, out Type genericInterface)
    {
        genericInterface = type
            .GetInterfaces()
            .FirstOrDefault(
                i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRecordMapper<>));

        return genericInterface is not null;
    }

    internal static IRecordMapper<T> GetMapper<T>() where T : new()
    {
        lock (_lockObject)
        {
            return (IRecordMapper<T>)GetMapperForType(typeof(T));
        }
    }

    internal static IRecordMapper GetMapperForType(Type type)
    {
        lock (_lockObject)
        {
            if (Instance._mappers.TryGetValue(type, out var m))
            {
                return m;
            }

            // no mapper registered for this type, so use the default mapper
            var getMethod = typeof(DefaultMapper).GetMethod(nameof(DefaultMapper.Get));
            var genericMethod = getMethod!.MakeGenericMethod(type);
            return (IRecordMapper)genericMethod.Invoke(null, null);
        }
    }

    public static T Map<T>(IRecord record) where T : new()
    {
        lock (_lockObject)
        {
            return GetMapper<T>().Map(record);
        }
    }

    public static void RegisterProvider<T>() where T : IMappingProvider, new()
    {
        lock (_lockObject)
        {
            var provider = new T();
            provider.CreateMappers(Instance);
        }
    }

    IMappingRegistry IMappingRegistry.RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder)
    {
        var builder = new MappingBuilder<T>();
        mappingBuilder(builder);
        var mapper = builder.Build();
        Register(mapper);
        return this;
    }
}
