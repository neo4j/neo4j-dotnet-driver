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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Controls what aspect of a graph entity is read when mapping a record field to a C# property.
/// </summary>
/// <remarks>
/// <para>
/// By default the mapper reads a named property from the entity (<see cref="Property"/>). Use
/// <see cref="NodeLabel"/> or <see cref="RelationshipType"/> when you need to capture graph-structural
/// information — such as the label of a node or the type of a relationship — rather than a stored property value.
/// </para>
/// <para>
/// Specify the mapping source via <see cref="MappingSourceAttribute"/> on a property or parameter,
/// or via the <c>mappingSource</c> parameter of
/// <see cref="IMappingBuilder{TObject}.Map{TProperty}(System.Linq.Expressions.Expression{System.Func{TObject,TProperty}},string,MappingSource,System.Func{object,TProperty},bool)"/>.
/// </para>
/// </remarks>
public enum MappingSource
{
    /// <summary>The value of the named property on the entity will be read.</summary>
    Property,

    /// <summary>
    /// Reads the type string of an <see cref="IRelationship"/> field.
    /// </summary>
    /// <remarks>
    /// The field in the record must be an <see cref="IRelationship"/>. The relationship's type string is then
    /// assigned to the target property. If the field is not a relationship the property is ignored.
    /// </remarks>
    RelationshipType,

    /// <summary>
    /// Reads the labels of an <see cref="INode"/> field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The field in the record must be an <see cref="INode"/>. The node's labels are then assigned to the target
    /// property using one of these rules:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// If the target property is a <c>string</c>, the labels are joined with a comma separator
    /// (e.g. <c>"Person,Employee"</c>).
    /// </description></item>
    /// <item><description>
    /// If the target property is a collection type such as <c>List&lt;string&gt;</c> or
    /// <c>IEnumerable&lt;string&gt;</c>, each label is added as a separate element.
    /// </description></item>
    /// </list>
    /// <para>If the field is not a node the property is ignored.</para>
    /// </remarks>
    NodeLabel
}
