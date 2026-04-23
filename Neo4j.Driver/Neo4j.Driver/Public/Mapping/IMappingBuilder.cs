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

/// <summary>Defines a builder for mapping objects from <see cref="IRecord"/>s.</summary>
/// <remarks>
/// <para>
/// Instances of this interface are provided by <see cref="IMappingRegistry.RegisterMapping{T}"/> and should not
/// be constructed directly. Obtain a registry from
/// <see cref="RecordObjectMapping.RegisterProvider(IMappingProvider)"/>.
/// </para>
/// <para>
/// The builder supports two overall strategies:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Override strategy:</b> call <see cref="UseDefaultMapping"/> first, then call <see cref="Map{TProperty}(
/// System.Linq.Expressions.Expression{System.Func{TObject,TProperty}},string,MappingSource,
/// System.Func{object,TProperty},bool)"/> for each property that needs non-default behaviour. This is the least
/// code when only a few properties differ from convention.
/// </description></item>
/// <item><description>
/// <b>Full control strategy:</b> call <see cref="MapWholeObject"/> with a function that constructs the object
/// entirely from the record. This replaces the default mapper entirely and is useful when the mapping logic is
/// complex or constructor-based inference is not desirable.
/// </description></item>
/// </list>
/// </remarks>
/// <typeparam name="TObject">The type of object to be mapped.</typeparam>
public interface IMappingBuilder<TObject>
{
    /// <summary>
    /// Applies the default mapping for the object. Later calls to mapping configuration methods will override the
    /// default mapping for specific properties.
    /// </summary>
    /// <remarks>
    /// Call this first when you want the default mapper to handle most properties and only need to customise a
    /// few. For example:
    /// <code language="csharp">
    /// registry.RegisterMapping&lt;Person&gt;(b => b
    ///     .UseDefaultMapping()
    ///     .Map(p => p.Labels, "person", MappingSource.NodeLabel));
    /// </code>
    /// </remarks>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> UseDefaultMapping();

    /// <summary>Defines a mapping from a field in the record to a property on the object.</summary>
    /// <param name="destination">The property to map to.</param>
    /// <param name="path">The key of the field in the record.</param>
    /// <param name="mappingSource">A value indicating the type of value to be mapped from the specified field.</param>
    /// <param name="converter">
    /// An optional converter function to convert the value from the field value to the type of the
    /// property.
    /// </param>
    /// <param name="optional">
    /// A value indicating whether the mapping is optional. If true, the mapping will not throw an
    /// exception if the field is not present in the record.
    /// </param>
    /// <typeparam name="TProperty">
    /// The type of the property being mapped. This type will be inferred from the
    /// <paramref name="destination"/> parameter.
    /// </typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        string path,
        MappingSource mappingSource = MappingSource.Property,
        Func<object, TProperty> converter = null,
        bool optional = false);

    /// <summary>Defines a mapping directly from the record to a property on the object.</summary>
    /// <param name="destination">The property to map to.</param>
    /// <param name="valueGetter">
    /// A function that accepts an <see cref="IRecord"/> and returns the value to be stored in the
    /// property.
    /// </param>
    /// <typeparam name="TProperty">
    /// The type of the property being mapped. This type will be inferred from the
    /// <paramref name="destination"/> parameter.
    /// </typeparam>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> Map<TProperty>(
        Expression<Func<TObject, TProperty>> destination,
        Func<IRecord, object> valueGetter);

    /// <summary>Defines a mapping from a record directly to an entire object.</summary>
    /// <remarks>
    /// Use this when the construction logic is complex enough that property-by-property mapping is impractical.
    /// The supplied function receives the full <see cref="IRecord"/> and is responsible for constructing and
    /// returning the complete object. When this method is called, the default mapper is not invoked at all —
    /// do not combine it with <see cref="UseDefaultMapping"/>.
    /// <code language="csharp">
    /// registry.RegisterMapping&lt;Address&gt;(b => b
    ///     .MapWholeObject(r => new Address(
    ///         r["street"].As&lt;string&gt;(),
    ///         r["city"].As&lt;string&gt;(),
    ///         r["postcode"].As&lt;string&gt;())));
    /// </code>
    /// </remarks>
    /// <param name="mappingFunction">A function that accepts an <see cref="IRecord"/> and returns the mapped object.</param>
    /// <returns>This instance for method chaining.</returns>
    IMappingBuilder<TObject> MapWholeObject(Func<IRecord, TObject> mappingFunction);
}
