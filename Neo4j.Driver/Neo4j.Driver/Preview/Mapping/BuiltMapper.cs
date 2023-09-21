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
using System.Reflection;

namespace Neo4j.Driver.Preview.Mapping;

internal class BuiltMapper<TObject> : IRecordMapper<TObject> where TObject : new()
{
    private IRecordPathFinder _pathFinder = new RecordPathFinder();
    private IMappingSourceGetter _mappingSourceGetter = new MappingSourceGetter();

    private Func<IRecord, TObject> _wholeObjectMapping;
    private readonly List<Action<TObject, IRecord>> _recordMappings = new();

    private readonly MethodInfo _asGenericMethod =
        typeof(ValueExtensions).GetMethod(nameof(ValueExtensions.As), new[] { typeof(object) });

    public TObject Map(IRecord record)
    {
        // if there's a whole-object mapping, use it, otherwise create a new object
        var obj = _wholeObjectMapping is not null
            ? _wholeObjectMapping(record)
            : new TObject();

        // if there are individual mappings for the properties, apply them
        foreach (var mapping in _recordMappings)
        {
            mapping(obj, record);
        }

        return obj;
    }

    public void AddWholeObjectMapping(Func<IRecord, TObject> mappingFunction)
    {
        _wholeObjectMapping = mappingFunction;
    }

    public void AddMappingBySetter(
        MethodInfo propertySetter,
        InternalMappingSource mappingSource,
        Func<object, object> converter = null)
    {
        // create the .As<TProperty> method we're going to use
        var propertyType = propertySetter.GetParameters()[0].ParameterType;
        var asMethod = _asGenericMethod.MakeGenericMethod(propertyType);
        var getter = _mappingSourceGetter.GetMappingDelegate(mappingSource);
        AddMapping(propertySetter, GetValue);

        object GetValue(IRecord record)
        {
            var found = getter(record, out var value);

            return value switch
            {
                null => null,

                // prioritise a custom converter if there is one
                _ when converter is not null => converter(value),

                // don't convert entities, just pass them through to be handled specially
                IEntity entity => entity,

                // special case: if they want to map a list to a string, convert to comma-separated
                IList list when propertyType == typeof(string) => string.Join(",", list.Cast<object>()),

                // if it's a list, map the individual items in the list
                IList list => CreateMappedList(list, propertyType, record),

                // otherwise, convert the value to the type of the property
                _ => asMethod.Invoke(null, new[] { value })
            };
        }
    }

    private IList CreateMappedList(IList list, Type desiredListType, IRecord record)
    {
        var newList = (IList)Activator.CreateInstance(desiredListType);
        var desiredItemType = desiredListType.GetGenericArguments()[0];
        var asMethod = _asGenericMethod.MakeGenericMethod(desiredItemType);
        foreach (var item in list)
        {
            // entities and dictionaries can use the same logic, we can make them both into dictionaries
            var dict = item switch
            {
                IEntity entity => entity.Properties,
                IReadOnlyDictionary<string, object> dictionary => dictionary,
                _ => null
            };

            if (dict is not null)
            {
                // if the item is an entity or dictionary, we need to make it into a record and then map that
                newList.Add(
                    RecordObjectMapping.GetMapperForType(desiredItemType)
                        .MapInternal(new DictAsRecord(dict, record)));
            }
            else
            {
                // otherwise, just convert the item to the type of the list
                newList.Add(asMethod.Invoke(null, new[] { item }));
            }
        }

        return newList;
    }

    public void AddMapping(
        MethodInfo propertySetter,
        Func<IRecord, object> valueGetter)
    {
        _recordMappings.Add(MapFromRecord);
        return;

        void MapFromRecord(TObject obj, IRecord record)
        {
            var value = valueGetter(record);
            if (value is null && record is DictAsRecord { Record: var parentRecord })
            {
                // try the path relative to the parent record
                value = valueGetter(parentRecord);
            }

            switch (value)
            {
                // if null is returned, leave the property as the default value: the record may not have the given field
                case null: return;

                // if the value is an entity, make it into a fake record and map that (indirectly recursive)
                case IEntity entity:
                    var destType = propertySetter.GetParameters()[0].ParameterType;
                    var newEntityDest = RecordObjectMapping
                        .GetMapperForType(destType)
                        .MapInternal(new DictAsRecord(entity.Properties, record));

                    propertySetter.Invoke(obj, new[] { newEntityDest });
                    return;

                // otherwise, just set the property to the value
                default:
                    propertySetter.Invoke(obj, new object[] { value });
                    return;
            }
        }
    }

    object IRecordMapper.MapInternal(IRecord record)
    {
        return Map(record);
    }
}
