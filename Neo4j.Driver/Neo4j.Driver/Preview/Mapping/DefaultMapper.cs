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

internal class DefaultMapper<T> : IRecordMapper<T> where T : new()
{
    private static Func<IRecord, T> _mapFunc;

    private static Func<IRecord, T> Initialise()
    {
        var type = typeof(T);
        var propSetters = new Dictionary<string, Action<T, object>>();
        var propTypes = new Dictionary<string, Type>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propInfo = prop;
            propSetters[prop.Name.ToLower()] = (obj, value) => propInfo.SetValue(obj, value);
            propTypes[prop.Name.ToLower()] = prop.PropertyType;
        }

        T Map(IRecord record)
        {
            var obj = new T();
            var node = record[0].As<INode>();
            foreach (var (key, value) in node.Properties)
            {
                if (propSetters.TryGetValue(key, out var setter))
                {
                    setter(obj, Convert.ChangeType(value, propTypes[key]));
                }
            }

            return obj;
        }

        return Map;
    }

    /// <inheritdoc />
    public T Map(IRecord record)
    {
        if (_mapFunc == null)
        {
            _mapFunc = Initialise();
        }

        return _mapFunc(record);
    }
}
