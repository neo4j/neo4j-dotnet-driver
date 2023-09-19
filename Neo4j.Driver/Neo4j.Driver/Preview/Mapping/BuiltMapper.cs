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
using System.Reflection;

namespace Neo4j.Driver.Preview.Mapping;

internal class BuiltMapper<TObject> : IRecordMapper<TObject> where TObject : new()
{
    private Func<IRecord, TObject> _wholeObjectMapping;
    private readonly List<Action<TObject, IRecord>> _recordMappings = new();
    private readonly HashSet<string> _usedPaths = new();

    private readonly MethodInfo _asGenericMethod =
        typeof(ValueExtensions).GetMethod(nameof(ValueExtensions.As), new[] { typeof(object) });

    private bool _recordMapBuilt;
    private Dictionary<string, Func<IRecord, object>> _recordMap;

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
        string sourcePath,
        Func<object, object> converter = null)
    {
        // create the .As<TProperty> method we're going to use
        var propertyType = propertySetter.GetParameters()[0].ParameterType;
        var asMethod = _asGenericMethod.MakeGenericMethod(propertyType);
        _usedPaths.Add(sourcePath.ToLower()); // keep a list of used keys to avoid unnecessary work later
        AddMapping(propertySetter, GetValue);

        object GetValue(IRecord record)
        {
            if (!_recordMap.ContainsKey(sourcePath.ToLower()))
            {
                // shape of the record may have changed since the map was built
                BuildRecordMap(record);
            }

            var value = _recordMap[sourcePath.ToLower()](record);

            return value switch
            {
                null => null,

                // don't convert entities, just pass them through to be handled specially
                IEntity entity => entity,

                // if it's a list, map the individual items in the list
                IList list => CreateMappedList(list, propertyType, record),

                // otherwise, convert the value to the type of the property
                _ => converter != null ? converter(value) : asMethod.Invoke(null, new[] { value })
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

        void MapFromRecord(TObject obj, IRecord record)
        {
            if (!_recordMapBuilt)
            {
                BuildRecordMap(record);
            }

            object value;
            try
            {
                value = valueGetter(record);
            }
            catch (KeyNotFoundException)
            {
                // this may happen if they tried to get the value from the record in a nested entity
                if (record is DictAsRecord nar)
                {
                    try
                    {
                        // we'll look in the record the entity came from
                        value = valueGetter(nar.Record);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return;
                    }
                }
                else
                {
                    // the record didn't have a key with that name, so just leave the property as the default value
                    return;
                }
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

    private void BuildRecordMap(IRecord example)
    {
        _recordMap = new Dictionary<string, Func<IRecord, object>>();

        foreach (var field in example.Values)
        {
            // root-level field names take precedence over nested field names, so we set them last
            var key = field.Key.ToLower();
            if (_usedPaths.Contains(key))
            {
                // add the name of the field as a possible mapping
                _recordMap[key] = r => r[field.Key];
            }

            // entities and dictionaries can use the same logic, we just need to get the properties
            // from the entity and then they're both dictionaries
            IReadOnlyDictionary<string, object> innerDict = null;
            Func<IRecord, IReadOnlyDictionary<string, object>> getDict = null;
            switch (field.Value)
            {
                case IEntity entity:
                    innerDict = entity.Properties;
                    getDict = r => r switch
                    {
                        DictAsRecord dar => dar.Record[field.Key].As<IEntity>().Properties,
                        _ => r[field.Key].As<IEntity>().Properties
                    };

                    break;

                case IReadOnlyDictionary<string, object> dict:
                    innerDict = dict;
                    getDict = r => r[field.Key].As<IReadOnlyDictionary<string, object>>();
                    break;
            }

            if (innerDict is null)
            {
                continue;
            }

            // if it's an entity or dictionary, we need to add all the keys in the entity/dictionary as possible mappings
            // with the format "field.key", as well as just "key" if it's not already used
            foreach (var dictKey in innerDict.Keys)
            {
                var path = $"{field.Key}.{dictKey}".ToLower();

                object Accessor(IRecord r) => getDict(r).TryGetValue(dictKey, out var value) ? value : null;

                if (_usedPaths.Contains(path))
                {
                    _recordMap.Add(path, Accessor);
                }

                var dictKeyPath = dictKey.ToLower();
                if (_usedPaths.Contains(dictKeyPath) && !_recordMap.ContainsKey(dictKeyPath))
                {
                    _recordMap.Add(dictKeyPath, Accessor);
                }
            }
        }

        _recordMapBuilt = true;
    }

    object IRecordMapper.MapInternal(IRecord record)
    {
        return Map(record);
    }
}
