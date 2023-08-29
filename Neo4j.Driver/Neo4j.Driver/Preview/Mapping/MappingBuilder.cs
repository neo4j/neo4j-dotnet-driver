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
using System.Linq.Expressions;
using System.Reflection;

namespace Neo4j.Driver.Preview.Mapping;

internal class MappingBuilder<TObject> : IMappingBuilder<TObject> where TObject : new()
{
    private class BuiltMapper : IRecordMapper<TObject>
    {
        public List<Action<TObject, IRecord>> Mappings { get; } = new();
        private HashSet<string> _usedKeys = new();

        private MethodInfo _asGenericMethod = typeof(ValueExtensions).GetMethod(
            nameof(ValueExtensions.As),
            new[]
            {
                typeof(object)
            });

        private bool _recordMapBuilt;
        private Dictionary<string, Func<IRecord, object>> _recordMap;

        public TObject Map(IRecord record)
        {
            var obj = new TObject();
            foreach (var mapping in Mappings)
            {
                mapping(obj, record);
            }

            return obj;
        }

        public void AddMapping<TProperty>(
            Expression<Func<TObject, TProperty>> destination,
            string sourceKey,
            Func<object, TProperty> converter = null)
        {
            var setter = GetPropertySetter(destination);

            // create the .As<TProperty> method we're going to use
            var asMethod = _asGenericMethod.MakeGenericMethod(typeof(TProperty));

            void DoMapping(TObject obj, IRecord record)
            {
                if (!_recordMapBuilt)
                {
                    BuildRecordMap(record);
                }

                var value = _recordMap[sourceKey](record);
                if (value == null)
                {
                    return;
                }

                value = converter != null ? converter(value) : asMethod.Invoke(null, new[] { value });

                setter.Invoke(obj, new[] { value });
            }

            Mappings.Add(DoMapping);
            _usedKeys.Add(sourceKey.ToLower()); // keep a list of used keys to avoid unnecessary work later
        }

        private void BuildRecordMap(IRecord example)
        {
            _recordMap = new();

            foreach (var field in example.Values)
            {
                var key = field.Key.ToLower();
                if (_usedKeys.Contains(key))
                {
                    // first simply add the name of the field as a possible mapping
                    _recordMap.Add(key, r => r[field.Key]);
                }

                IReadOnlyDictionary<string, object> innerDict = null;
                Func<IRecord, IReadOnlyDictionary<string, object>> getDict = null;

                // nodes and dictionaries can use the same logic, we just need to get the properties
                // from the node and then they're both dictionaries
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
                else
                {
                    continue;
                }

                // if it's a node or dictionary, we need to add all the keys in the node/dictionary as possible mappings
                // with the format "field.key", as well as just "key" if it's not already used
                foreach (var dictKey in innerDict.Keys)
                {
                    string path = $"{field.Key}.{dictKey}".ToLower();

                    object Accessor(IRecord r) =>
                        getDict(r).TryGetValue(dictKey, out var value)
                            ? value
                            : null;

                    if (_usedKeys.Contains(path))
                    {
                        _recordMap.Add(path, Accessor);
                    }

                    if (_usedKeys.Contains(dictKey.ToLower()) && !_recordMap.ContainsKey(dictKey))
                    {
                        _recordMap.Add(dictKey.ToLower(), Accessor);
                    }
                }
            }

            _recordMapBuilt = true;
        }
    }

    private readonly BuiltMapper _builtMapper = new();

    public IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        string sourceKey,
        Func<object, TProperty> converter = null)
    {
        _builtMapper.AddMapping(destination, sourceKey, converter);
        return this;
    }

    internal IRecordMapper<TObject> Build()
    {
        return _builtMapper;
    }

    private static MethodInfo GetPropertySetter<TProperty>(Expression<Func<TObject, TProperty>> destination)
    {
        var body = destination.Body.ToString();
        if (destination.Body is not MemberExpression member)
        {
            throw new ArgumentException("Expression is not a member expression", body);
        }

        if (member.Member is not PropertyInfo prop)
        {
            throw new ArgumentException("Expression is not a property expression", body);
        }

        var setter = prop.GetSetMethod();
        if (setter == null)
        {
            throw new ArgumentException("Property does not have a setter", body);
        }

        return setter;
    }
}
