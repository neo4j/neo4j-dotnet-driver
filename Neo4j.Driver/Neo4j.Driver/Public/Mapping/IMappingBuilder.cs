// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Defines a builder for mapping objects from <see cref="IRecord"/>s.
/// </summary>
/// <typeparam name="TObject">The type of object to be mapped.</typeparam>
public interface IMappingBuilder<TObject>
{
    /// <summary>
    /// Applies the default mapping for the object. Later calls to mapping configuration methods will override
    /// the default mapping.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> UseDefaultMapping();

    /// <summary>
    /// Defines a mapping from a field in the record to a property on the object.
    /// </summary>
    /// <param name="destination">The property to map to.</param>
    /// <param name="path">The key of the field in the record.</param>
    /// <param name="entityMappingSource">A value indicating the type of value to be mapped from the specified field.
    /// </param>
    /// <param name="converter">An optional converter function to convert the value from the field value
    /// to the type of the property.</param>
    /// <typeparam name="TProperty">The type of the property being mapped. This type will be inferred from the
    /// <paramref name="destination"/> parameter.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        string path,
        EntityMappingSource entityMappingSource = EntityMappingSource.Property,
        Func<object, TProperty> converter = null,
        bool optional = false);

    /// <summary>
    /// Defines a mapping directly from the record to a property on the object.
    /// </summary>
    /// <param name="destination">The property to map to.</param>
    /// <param name="valueGetter">A function that accepts an <see cref="IRecord"/> and returns the value to be
    /// stored in the property.</param>
    /// <typeparam name="TProperty">The type of the property being mapped. This type will be inferred from the
    /// <paramref name="destination"/> parameter.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        Func<IRecord, object> valueGetter);

    /// <summary>
    /// Defines a mapping from a the record directly to the entire object.
    /// </summary>
    /// <param name="mappingFunction">A function that accepts an <see cref="IRecord"/> and returns the mapped
    /// object.</param>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> MapWholeObject(Func<IRecord, TObject> mappingFunction);
}
