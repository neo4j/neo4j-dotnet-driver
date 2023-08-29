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
using System.Reflection;

namespace Neo4j.Driver.Preview.Mapping;

public interface IMappingRegistry
{
    IMappingRegistry RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder) where T : new();
}

public class RecordMappers : IMappingRegistry
{
    private static readonly RecordMappers Instance = new();

    private readonly Dictionary<Type, IRecordMapper> _mappers = new();

    public static void Register<T>(T recordMapper) where T : IRecordMapper, new()
    {
        Register<T>(new T());
    }

    public static void Register(IRecordMapper mapper)
    {
        if (!IsValidMapper(mapper.GetType(), out var genericInterface))
        {
            throw new ArgumentException("Mapper type must implement IRecordMapper<>");
        }

        var destinationType = genericInterface.GetGenericArguments()[0];
        Instance._mappers[destinationType] = mapper;
    }

    public static void RegisterAllFromAssembly(Assembly assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        foreach (var type in assembly.GetTypes())
        {
            if (IsValidMapper(type, out _))
            {
                Register((IRecordMapper)Activator.CreateInstance(type));
            }
        }
    }

    private static bool IsValidMapper(Type type, out Type genericInterface)
    {
        genericInterface = type
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRecordMapper<>));

        return genericInterface is not null;
    }

    public static bool TryGetMapper<T>(out IRecordMapper<T> mapper)
    {
        if (Instance._mappers.TryGetValue(typeof(T), out var m))
        {
            mapper = (IRecordMapper<T>)m;
            return true;
        }

        mapper = null;
        return false;
    }

    public static void RegisterProvider<T>() where T : IMappingProvider, new()
    {
        var provider = new T();
        provider.CreateMappers(Instance);
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
