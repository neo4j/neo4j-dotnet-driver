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
using System.Linq.Expressions;
using System.Reflection;

namespace Neo4j.Driver.Preview.Mapping;

internal class MappingBuilder<TObject> : IMappingBuilder<TObject> where TObject : new()
{
    private readonly BuiltMapper<TObject> _builtMapper = new();

    internal void Map(
        MethodInfo propertySetter,
        InternalMappingSource mappingSource)
    {
        _builtMapper.AddMappingBySetter(propertySetter, mappingSource);
    }

    public IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        string path,
        EntityMappingSource entityMappingSource = EntityMappingSource.Property,
        Func<object, TProperty> converter = null)
    {
        var propertySetter = GetPropertySetter(destination);
        _builtMapper.AddMappingBySetter(
            propertySetter,
            new InternalMappingSource(path, entityMappingSource),
            converter is null ? null : o => converter.Invoke(o));

        return this;
    }

    /// <inheritdoc />
    public IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        Func<IRecord, object> valueGetter)
    {
        var propertySetter = GetPropertySetter(destination);
        _builtMapper.AddMapping(propertySetter, valueGetter);
        return this;
    }

    public IMappingBuilder<TObject> MapWholeObject(Func<IRecord, TObject> mappingFunction)
    {
        _builtMapper.AddWholeObjectMapping(mappingFunction);
        return this;
    }

    /// <inheritdoc />
    public IMappingBuilder<TObject> UseDefaultMapping()
    {
        _builtMapper.AddWholeObjectMapping(r => DefaultMapper.Get<TObject>().Map(r));
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
