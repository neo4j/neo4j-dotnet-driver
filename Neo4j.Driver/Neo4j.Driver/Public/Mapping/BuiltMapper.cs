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

namespace Neo4j.Driver.Mapping;

internal class BuiltMapper<T> : IRecordMapper<T>
{
    private readonly IMappableValueProvider _mappableValueProvider = new MappableValueProvider();

    private Func<IRecord, T> _wholeObjectMapping;
    private readonly List<Action<T, IRecord>> _propertyMappings = new();
    public HashSet<MethodInfo> MappedSetters { get; } = [];

    public T Map(IRecord record)
    {
        T result;
        // if there's a whole-object mapping, use it, otherwise create a new object
        try
        {
            result = _wholeObjectMapping is not null
                ? _wholeObjectMapping(record)
                : CreateObject<T>();
        }
        catch (Exception ex)
        {
            throw new MappingFailedException(
                $"Cannot map record to type {typeof(T).Name} " +
                $"because the mapping function threw an exception.",
                ex);
        }

        // if there are individual mappings for the properties, apply them
        foreach (var mapping in _propertyMappings)
        {
            mapping(result, record);
        }

        return result;
    }

    private static TObject CreateObject<TObject>()
    {
        // check for parameterless constructor
        var constructor = typeof(TObject).GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationException(
                $"Cannot create an instance of type {typeof(TObject).Name} " +
                $"because it does not have a parameterless constructor.");
        }

        return (TObject)constructor.Invoke([]);
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
                mapping = parameter.GetEntityMappingInfo()
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
                    throw new MappingFailedException(
                        $"Cannot map record to type {typeof(T).Name} because the record does not " +
                        $"contain a value for the constructor parameter '{p.parameter.Name}'.");
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
        AddMapping(propertySetter, GetValue, entityMappingInfo.Optional);
        return;

        // this part happens every time a record is mapped
        object GetValue(IRecord record)
        {
            return _mappableValueProvider.GetConvertedValue(record, entityMappingInfo, propertyType, converter);
        }
    }

    public void AddMapping(
        MethodInfo propertySetter,
        Func<IRecord, object> valueGetter,
        bool optional = false)
    {
        // this part only happens once, at the time of building the mapper
        _propertyMappings.Add(MapFromRecord);
        MappedSetters.Add(propertySetter);
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
                propertySetter.Invoke(obj, [mappableValue]);
            }
            else if(!optional)
            {
                // throw because we couldn't find a value for the property
                var propertyName = propertySetter.Name.Substring("set_".Length);
                throw new MappingFailedException(
                    $"Cannot map record to type {typeof(T).Name} " +
                    $"because the record does not contain a value for the property '{propertyName}'.");
            }
        }
    }
}
