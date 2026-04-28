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
using Neo4j.Driver.Internal.Mapping;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Instructs the default mapper to use a different record field name (or dot-separated path) when mapping a value
/// to the marked property or parameter. This attribute does not affect custom-defined mappers.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when the record field name does not match the C# property or parameter name, or when
/// global identifier translation via <see cref="RecordObjectMapping.TranslateIdentifiers(bool)"/> is active
/// but you need to bypass it for a specific member.
/// </para>
/// <para>
/// A <b>simple path</b> is a single record key: <c>[MappingSource("first_name")]</c>.
/// </para>
/// <para>
/// A <b>dot-separated path</b> lets you read a property from a nested entity or dictionary column:
/// <c>[MappingSource("person.name")]</c> reads the <c>name</c> property from the record field <c>person</c>,
/// where <c>person</c> is a node, relationship, or dictionary. All segments are matched case-sensitively.
/// </para>
/// <para>
/// When a <see cref="MappingSource"/> other than <see cref="MappingSource.Property"/> is required (for example
/// to read a node's labels), use the two-argument constructor.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class MappingSourceAttribute : MappingBindingsAttribute
{
    /// <summary>
    /// Instructs the default mapper to use the specified field name or dot-separated path when mapping to the
    /// marked property or parameter.
    /// </summary>
    /// <param name="path">
    /// The record field key to read from. May be a dot-separated path of the form <c>field.nestedKey</c>,
    /// where <c>field</c> resolves to a node, relationship, or dictionary column in the record and
    /// <c>nestedKey</c> is a property within that entity. All segments are matched case-sensitively.
    /// </param>
    public MappingSourceAttribute(string path)
    {
        Path = path;
    }
    /// <summary>
    /// Instructs the default mapper to use the specified record field and mapping source when mapping to the
    /// marked property or parameter.
    /// </summary>
    /// <param name="key">
    /// The record field key to read from. May be a dot-separated path of the form <c>field.nestedKey</c>.
    /// All segments are matched case-sensitively.
    /// </param>
    /// <param name="mappingSource">
    /// The aspect of the entity to read. Use <see cref="MappingSource.NodeLabel"/> to read a node's labels,
    /// or <see cref="MappingSource.RelationshipType"/> to read a relationship's type string.
    /// </param>
    public MappingSourceAttribute(string key, MappingSource mappingSource) 
    {
        Path = key;
        Source = mappingSource;
    }

    /// <inheritdoc/>
    public override void Mutate(MappingBinding binding)
    {
        base.Mutate(binding);
        binding.Explicit = true;
    }
}
