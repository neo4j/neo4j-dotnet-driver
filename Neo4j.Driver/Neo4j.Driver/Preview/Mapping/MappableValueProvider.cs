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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Preview.Mapping;

internal interface IMappableValueProvider
{
    bool TryGetMappableValue(
        IRecord record,
        Func<IRecord, object> valueGetter,
        Type desiredType,
        out object result);

    object GetConvertedValue(
        IRecord record,
        EntityMappingInfo mappingInfo,
        Type propertyType,
        Func<object, object> converter);
}

internal class MappableValueProvider : IMappableValueProvider
{
    private readonly IMappedListCreator _mappedListCreator;
    private readonly IMappingSourceDelegateBuilder _mappingSourceDelegateBuilder;
    private readonly IRecordObjectMapping _recordObjectMapping;

    internal MappableValueProvider(
        IMappedListCreator mappedListCreator = null,
        IMappingSourceDelegateBuilder mappingSourceDelegateBuilder = null,
        IRecordObjectMapping recordObjectMapping = null)
    {
        _mappedListCreator = mappedListCreator ?? new MappedListCreator();
        _mappingSourceDelegateBuilder = mappingSourceDelegateBuilder ?? new MappingSourceDelegateBuilder();
        _recordObjectMapping = recordObjectMapping ?? RecordObjectMapping.Instance;
    }

    public bool TryGetMappableValue(
        IRecord record,
        Func<IRecord, object> valueGetter,
        Type desiredType,
        out object result)
    {
        var value = valueGetter(record);
        if (value is null && record is DictAsRecord { Record: var parentRecord })
        {
            // try the path relative to the parent record
            value = valueGetter(parentRecord);
        }

        result = null;

        switch (value)
        {
            // if null is returned, leave the property as the default value: the record may not have the given field
            case null: return false;

            // if the value is an entity, make it into a fake record and map that (indirectly recursive)
            case IEntity entity:
                var dictAsRecord = new DictAsRecord(entity.Properties, record);
                result = _recordObjectMapping.Map(dictAsRecord, desiredType);
                return true;

            // if the value is a dictionary, make it into a fake record and map that (indirectly recursive)
            case IReadOnlyDictionary<string, object> dictionary:
                dictAsRecord = new DictAsRecord(dictionary, record);
                result = _recordObjectMapping.Map(dictAsRecord, desiredType);
                return true;

            // otherwise, just return the value
            default:
                result = value;
                return true;
        }
    }

    public object GetConvertedValue(
        IRecord record,
        EntityMappingInfo mappingInfo,
        Type propertyType,
        Func<object, object> converter = null)
    {
        var getter = _mappingSourceDelegateBuilder.GetMappingDelegate(mappingInfo);
        var found = getter(record, out var value);
        return value switch
        {
            _ when !found => null,
            null => null,

            // prioritise a custom converter if there is one
            _ when converter is not null => converter(value),

            // don't convert entities and dictionaries, just return them as-is
            IEntity or IDictionary => value,

            // special case: if they want to map a list to a string, convert to comma-separated
            ICollection list when propertyType == typeof(string) => string.Join(",", list.Cast<object>()),

            // if it's a list, map the individual items in the list
            ICollection list => _mappedListCreator.CreateMappedList(list, propertyType, record),

            // otherwise, convert the value to the type of the property
            _ => value.AsType(propertyType)
        };
    }
}
