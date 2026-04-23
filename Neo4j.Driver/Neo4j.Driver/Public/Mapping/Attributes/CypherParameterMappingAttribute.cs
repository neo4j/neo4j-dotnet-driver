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
/// Overrides the Cypher parameter name used when the containing object is passed as a query parameter.
/// </summary>
/// <remarks>
/// <para>
/// When a C# object is passed as a parameter to a Cypher query, property names are used as the parameter
/// key names by default. Apply this attribute to use a different key for a specific property.
/// </para>
/// <para>
/// This attribute controls the <em>object-to-parameter</em> direction (C# → Cypher). It is unrelated to the
/// <em>record-to-object</em> direction. To control how a record field is mapped to a property during result
/// reading, use <see cref="MappingSourceAttribute"/> instead.
/// </para>
/// <para>
/// If <see cref="RecordObjectMapping.TranslateIdentifiers(bool)"/> has been called with
/// <c>translateCypherParameters: true</c>, names are translated automatically and this attribute is
/// only needed to override specific properties that should not follow the global convention.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class CypherParameterMappingAttribute : MappingBindingsAttribute
{
    /// <summary>
    /// Initializes the attribute with the Cypher parameter key name to use for this property.
    /// </summary>
    /// <param name="cypherParameterName">The Cypher parameter key name to use for this property.</param>
    public CypherParameterMappingAttribute(string cypherParameterName) 
    {
        CypherParameterName = cypherParameterName;
    }
}
