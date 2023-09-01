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

internal class BuiltMapper<TObject> : IRecordMapper<TObject> where TObject : new()
{
    private readonly List<Action<TObject, IRecord>> _recordMappings = new();
    private HashSet<string> _usedPaths = new();

    private MethodInfo _asGenericMethod =
        typeof(ValueExtensions).GetMethod(nameof(ValueExtensions.As), new[] { typeof(object) });

    private bool _recordMapBuilt;
    private Dictionary<string, Func<IRecord, object>> _recordMap;

    public TObject Map(IRecord record)
    {
        var obj = new TObject();
        foreach (var mapping in _recordMappings)
        {
            mapping(obj, record);
        }

        return obj;
    }

    public void AddMappingBySetter(
        MethodInfo propertySetter,
        string sourcePath,
        Func<object, object> converter = null)
    {
        // create the .As<TProperty> method we're going to use
        var asMethod = _asGenericMethod.MakeGenericMethod(propertySetter.GetParameters()[0].ParameterType);
        _usedPaths.Add(sourcePath.ToLower()); // keep a list of used keys to avoid unnecessary work later

        object GetValue(IRecord r)
        {
            var value = _recordMap[sourcePath.ToLower()](r);

            return value switch
            {
                // if the value is null, just return null
                null => null,

                // don't convert nodes, just pass them through to be handled specially
                INode node => node,

                // otherwise, convert the value to the type of the property
                _ => converter != null ? converter(value) : asMethod.Invoke(null, new[] { value })
            };
        }

        AddMapping(propertySetter, GetValue);
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
            catch (Exception ex)
            {
                // this may happen if they tried to get the value from the record in a nested node
                throw new Neo4jException($"Error getting value for {propertySetter.Name}", ex);
            }

            switch (value)
            {
                // if null is returned, leave the property as the default value, the record may not have the given field
                case null: return;

                // if the value is a node, make it into a fake record and map that (indirectly recursive)
                case INode node:
                    var destType = propertySetter.GetParameters()[0].ParameterType;
                    var newNodeDest = RecordObjectMapping.GetMapperForType(destType).MapInternal(node.AsRecord());
                    propertySetter.Invoke(obj, new[] { newNodeDest });
                    return;

                // otherwise, just set the property to the value
                default: propertySetter.Invoke(obj, new object[] { value });
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

            // nodes and dictionaries can use the same logic, we just need to get the properties
            // from the node and then they're both dictionaries
            IReadOnlyDictionary<string, object> innerDict = null;
            Func<IRecord, IReadOnlyDictionary<string, object>> getDict = null;
            if (field.Value is INode node)
            {
                innerDict = node.Properties;
                getDict = r => r[field.Key].As<INode>().Properties;
            }
            else if (field.Value is IReadOnlyDictionary<string, object> dict)
            {
                innerDict = dict;
                getDict = r => r[field.Key].As<IReadOnlyDictionary<string, object>>();
            }

            if (innerDict is not null)
            {
                // if it's a node or dictionary, we need to add all the keys in the node/dictionary as possible mappings
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
        }

        _recordMapBuilt = true;
    }

    object IRecordMapper.MapInternal(IRecord record)
    {
        return Map(record);
    }
}
