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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains methods for registering a mapping with the global mapping configuration.
/// </summary>
public interface IMappingRegistry
{
    /// <summary>
    /// Registers a mapping for the given type.
    /// </summary>
    /// <param name="mappingBuilder">This method will be called, passing a parameter that contains
    /// a fluent API for defining the mapping.</param>
    /// <typeparam name="T">The type to be mapped.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingRegistry RegisterMapping<T>(Action<IMappingBuilder<T>> mappingBuilder);
}

internal interface IRecordObjectMapping
{
    object Map(IRecord record, Type type);
    TResult MapFromBlueprint<TResult>(IRecord record, TResult blueprint);
}

/// <summary>
/// Controls global record mapping configuration.
/// </summary>
public class RecordObjectMapping : IMappingRegistry, IRecordObjectMapping
{
    internal static RecordObjectMapping Instance { get; private set; } = new();

    private readonly Dictionary<Type, object> _mappers = new();
    private readonly Dictionary<Type, MethodInfo> _mapMethods = new();

    private RecordObjectMapping()
    {
    }

    internal static void Reset()
    {
        // discard the current instance and create a new one, which will have no mappers registered
        Instance = new RecordObjectMapping();
        DefaultMapper.Reset();
    }

    /// <summary>
    /// Registers a single record mapper. This will replace any existing mapper for the same type.
    /// </summary>
    /// <param name="mapper">The mapper. This must implement <see cref="IRecordMapper{T}"/> for the type
    /// to be mapped.</param>
    /// <exception cref="ArgumentException">The provided <paramref name="mapper"/> does not implement
    /// IRecordMapper{T}.</exception>
    public static void Register<T>(IRecordMapper<T> mapper)
    {
        Instance._mappers[typeof(T)] = mapper;
    }

    private static object GetMapperForType(Type type)
    {
        if (Instance._mappers.TryGetValue(type, out var m))
        {
            return m;
        }

        // no mapper registered for this type, so use the default mapper
        var openGenericMethod = typeof(DefaultMapper).GetMethod(nameof(DefaultMapper.Get));
        var closedGenericMethod = openGenericMethod!.MakeGenericMethod(type);
        return closedGenericMethod.Invoke(null, [null]);
    }

    /// <summary>
    /// Maps a record to an object of the given type according to the global mapping configuration.
    /// </summary>
    /// <param name="record">The record to be mapped.</param>
    /// <typeparam name="T">The type of object to be mapped.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T Map<T>(IRecord record)
    {
        var mapper = (IRecordMapper<T>)GetMapperForType(typeof(T));
        return mapper.Map(record);
    }

    /// <summary>
    /// Registers a mapping provider. This will call <see cref="IMappingProvider.CreateMappers"/> on the
    /// provider, allowing it to register any mappers it wishes.
    /// </summary>
    /// <typeparam name="T">The type of the mapping provider.</typeparam>
    public static void RegisterProvider<T>() where T : IMappingProvider, new()
    {
        RegisterProvider(new T());
    }

    /// <summary>
    /// Registers a mapping provider. This will call <see cref="IMappingProvider.CreateMappers"/> on the
    /// provider, allowing it to register any mappers it wishes.
    /// </summary>
    /// <param name="provider"></param>
    public static void RegisterProvider(IMappingProvider provider)
    {
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

    private MethodInfo GetMapMethodForType(Type type)
    {
        if (_mapMethods.TryGetValue(type, out var method))
        {
            return method;
        }

        var typedInterface = typeof(IRecordMapper<>).MakeGenericType(type);
        var mapMethod = typedInterface.GetMethod(nameof(IRecordMapper<object>.Map));
        _mapMethods[type] = mapMethod;
        return mapMethod;
    }

    /// <summary>
    /// Maps a record to an object of the given type according to the global mapping configuration.
    /// </summary>
    /// <param name="record">The record to be mapped.</param>
    /// <param name="type">The type of object to be mapped.</param>
    /// <returns>The mapped object.</returns>
    public static object Map(IRecord record, Type type)
    {
        return ((IRecordObjectMapping)Instance).Map(record, type);
    }

    /// <summary>
    /// Maps a record to a new object of the same type as the provided blueprint object.
    /// </summary>
    /// <param name="record">The record to be mapped.</param>
    /// <param name="blueprint">An object of the type to be mapped, used to determine the type of the
    /// object to be created. Any values in the properties of the blueprint object will be discarded.</param>
    /// <typeparam name="T">The type of object that will be mapped.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T MapFromBlueprint<T>(IRecord record, T blueprint)
    {
        return ((IRecordObjectMapping)Instance).MapFromBlueprint(record, blueprint);
    }

    object IRecordObjectMapping.Map(IRecord record, Type type)
    {
        var mapMethod = Instance.GetMapMethodForType(type);
        var mapperForType = GetMapperForType(type);

        try
        {
            return mapMethod.Invoke(mapperForType, [record]);
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException tie ? tie.InnerException : ex;
            throw new MappingFailedException($"Failed to map record to type {type.Name}.", inner);
        }
    }

    T IRecordObjectMapping.MapFromBlueprint<T>(IRecord record, T blueprint)
    {
        return (T)Map(record, typeof(T));
    }
}
