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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// This class wraps the <see cref="BuiltMapper{T}"/> and exposes a fluent API for building the mapper.
/// </summary>
internal class MappingBuilder<T> : IMappingBuilder<T>
{
    private readonly BuiltMapper<T> _builtMapper = new();

    internal void Map(
        MethodInfo propertySetter,
        EntityMappingInfo entityMappingInfo)
    {
        _builtMapper.AddMappingBySetter(propertySetter, entityMappingInfo);
    }

    public IMappingBuilder<T> Map<TProperty>(
        Expression<Func<T, TProperty>> destination,
        string path,
        EntityMappingSource entityMappingSource = EntityMappingSource.Property,
        Func<object, TProperty> converter = null,
        bool optional = false)
    {
        _builtMapper.AddMappingBySetter(
            GetPropertySetter(destination),
            new EntityMappingInfo(path, entityMappingSource, optional),
            converter is null ? null : o => converter.Invoke(o));

        return this;
    }

    public IMappingBuilder<T> Map<TProperty>(
        Expression<Func<T, TProperty>> destination,
        Func<IRecord, object> valueGetter)
    {
        var propertySetter = GetPropertySetter(destination);
        _builtMapper.AddMapping(propertySetter, valueGetter);
        return this;
    }

    public IMappingBuilder<T> MapWholeObject(Func<IRecord, T> mappingFunction)
    {
        _builtMapper.AddWholeObjectMapping(mappingFunction);
        return this;
    }

    internal IMappingBuilder<T> UseConstructor(ConstructorInfo constructor)
    {
        _builtMapper.AddConstructorMapping(constructor);
        return this;
    }

    /// <inheritdoc />
    public IMappingBuilder<T> UseDefaultMapping()
    {
        _builtMapper.AddWholeObjectMapping(r => DefaultMapper.Get<T>(_builtMapper.MappedSetters).Map(r));
        return this;
    }

    internal IRecordMapper<T> Build()
    {
        return _builtMapper;
    }

    /// <summary>
    /// Given an expression that represents a property (e.g. <c>o => o.Name</c>),
    /// return the <see cref="MethodInfo"/> for the setter for that property.
    /// </summary>
    private static MethodInfo GetPropertySetter<TProperty>(Expression<Func<T, TProperty>> destination)
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
