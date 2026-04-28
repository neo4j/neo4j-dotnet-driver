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

using Neo4j.Driver.Internal.Mapping;

namespace Neo4j.Driver.Mapping;

/// <summary>Contains extensions for entities such as nodes and relationships.</summary>
public static class EntityExtensions
{
    /// <summary>
    /// Wraps the entity's properties in an <see cref="IRecord"/> so that it can be mapped to a C# object
    /// using the standard mapping API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this when you have a standalone <see cref="INode"/> or <see cref="IRelationship"/> — for example,
    /// retrieved from an <see cref="IPath"/> or a result column — and you want to map it to a typed C# object
    /// without going through a full record. After calling <c>AsRecord()</c>, you can call
    /// <see cref="RecordExtensions.AsObject{T}"/> on the result:
    /// </para>
    /// <code language="csharp">
    /// INode node = ...; // obtained from a path or similar
    /// var person = node.AsRecord().AsObject&lt;Person&gt;();
    /// </code>
    /// <para>
    /// The fields of the virtual record are only the entity's properties. Node labels and relationship types are
    /// not exposed through the <see cref="IRecord"/> returned by <c>AsRecord()</c>. If you need to map labels or
    /// relationship types, use <see cref="MappingSource.NodeLabel"/> or
    /// <see cref="MappingSource.RelationshipType"/> when mapping directly from a query result column whose value is
    /// an <see cref="INode"/> or <see cref="IRelationship"/>, rather than from <c>entity.AsRecord()</c>.
    /// </para>
    /// <para>
    /// See
    /// <a href="~/articles/mapping-overview.md">Mapping query results to objects</a> and
    /// <a href="~/articles/mapping-configuration.md">Configuring the mapping system</a>.
    /// </para>
    /// </remarks>
    /// <param name="entity">The node or relationship to wrap.</param>
    /// <returns>An <see cref="IRecord"/> backed by the entity's properties.</returns>
    public static IRecord AsRecord(this IEntity entity)
    {
        return new DictAsRecord(entity, null);
    }
}
