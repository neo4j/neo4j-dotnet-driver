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

internal class BuiltMapper<T> : IRecordMapper<T>
{
    private readonly IMappableValueProvider _mappableValueProvider = new MappableValueProvider();

    private Func<IRecord, T> _wholeObjectMapping;
    private readonly List<Action<T, IRecord>> _propertyMappings = new();

    public T Map(IRecord record)
    {
        // if there's a whole-object mapping, use it, otherwise create a new object
        var obj = _wholeObjectMapping is not null
            ? _wholeObjectMapping(record)
            : CreateObject<T>();

        // if there are individual mappings for the properties, apply them
        foreach (var mapping in _propertyMappings)
        {
            mapping(obj, record);
        }

        return obj;
    }

    private static T CreateObject<T>()
    {
        // check for parameterless constructor
        var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationException(
                $"Cannot create an instance of type {typeof(T).Name} " +
                $"because it does not have a parameterless constructor.");
        }

        return (T)constructor.Invoke(Array.Empty<object>());
    }

    public void AddWholeObjectMapping(Func<IRecord, T> mappingFunction)
    {
        _wholeObjectMapping = mappingFunction;
    }

    public void AddConstructorMapping(ConstructorInfo constructorInfo)
    {
        // this part only happens once, at the time of building the mapper
        var parameters = constructorInfo.GetParameters();
        var paramMappings = parameters.Select(
            parameter => new
            {
                parameter,
                mapping = parameter.GetCustomAttribute<MappingSourceAttribute>()?.EntityMappingInfo ??
                    new EntityMappingInfo(parameter.Name, EntityMappingSource.Property)
            });

        _wholeObjectMapping = MapFromRecord;
        return;

        // this part happens every time a record is mapped
        T MapFromRecord(IRecord record)
        {
            var args = new List<object>();
            foreach (var p in paramMappings)
            {
                var success = _mappableValueProvider.TryGetMappableValue(
                    record,
                    r => _mappableValueProvider.GetConvertedValue(r, p.mapping, p.parameter.ParameterType, null),
                    p.parameter.ParameterType,
                    out var mappable);

                if (!success)
                {
                    throw new InvalidOperationException(
                        $"Cannot map record to type {typeof(T).Name} " +
                        $"because the record does not contain a value for the parameter '{p.parameter.Name}'.");
                }

                args.Add(mappable);

            }

            return (T)constructorInfo.Invoke(args.ToArray());
        }
    }

    public void AddMappingBySetter(
        MethodInfo propertySetter,
        EntityMappingInfo entityMappingInfo,
        Func<object, object> converter = null)
    {
        // this part only happens once, at the time of building the mapper
        var propertyType = propertySetter.GetParameters()[0].ParameterType;
        AddMapping(propertySetter, GetValue);
        return;

        // this part happens every time a record is mapped
        object GetValue(IRecord record)
        {
            return _mappableValueProvider.GetConvertedValue(record, entityMappingInfo, propertyType, converter);
        }
    }

    public void AddMapping(
        MethodInfo propertySetter,
        Func<IRecord, object> valueGetter)
    {
        // this part only happens once, at the time of building the mapper
        _propertyMappings.Add(MapFromRecord);
        return;

        // this part happens every time a record is mapped
        void MapFromRecord(T obj, IRecord record)
        {
            var mappableValueFound = _mappableValueProvider.TryGetMappableValue(
                record,
                valueGetter,
                propertySetter.GetParameters()[0].ParameterType,
                out var mappableValue);

            if (mappableValueFound)
            {
                propertySetter.Invoke(obj, new[] { mappableValue });
            }
        }
    }
}
