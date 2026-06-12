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

using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver;

/// <summary>An executable query, i.e. the queries' text and its parameters.</summary>
public class Query
{
    /// <summary>Create a query with no query parameters.</summary>
    /// <param name="text">The query's text</param>
    public Query(string text) : this(text, (object)null)
    {
    }

    /// <summary>Create a query with parameters as an object.</summary>
    /// <param name="text">The query's text.</param>
    /// <param name="parameters">
    /// The query parameters. The driver converts this object to a Cypher parameter map as follows:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Anonymous types and POCOs:</b> each public property becomes a Cypher parameter, with
    ///     the property name used as the key. Nested objects become Cypher maps; collections and
    ///     arrays become Cypher lists.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Dictionary types</b> (<see cref="IDictionary{TKey,TValue}"/> with string keys,
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}"/>, or <see cref="IEnumerable{T}"/> of
    ///     <see cref="KeyValuePair{TKey,TValue}"/>): treated as the parameter map.
    ///   </description></item>
    /// </list>
    /// <para>
    ///   <b>Renaming parameters:</b> decorate a property with
    ///   <see cref="Mapping.CypherParameterMappingAttribute"/> to override the key used in the
    ///   Cypher parameter map. For full control, implement
    ///   <see cref="Mapping.IMappingBindingMutator"/> on a custom attribute.
    /// </para>
    /// <para>
    ///   <b>Global name translation:</b> if
    ///   <see cref="Mapping.RecordObjectMapping"/><c>.TranslateIdentifiers</c> has been called with
    ///   <c>translateCypherParameters: true</c>, the configured
    ///   <see cref="Mapping.ConventionTranslation.IConventionTranslator"/> is applied to top-level
    ///   property names that do not carry an explicit
    ///   <see cref="Mapping.CypherParameterMappingAttribute"/>.
    /// </para>
    /// </param>
    public Query(string text, object parameters)
        : this(text, parameters.ToCypherParameterDictionary())
    {
    }

    /// <summary>Create a query</summary>
    /// <param name="text">The query's text</param>
    /// <param name="parameters">
    /// The query's parameters, whose values should not be changed while the query is used in a
    /// session/transaction.
    /// </param>
    public Query(string text, IDictionary<string, object> parameters)
    {
        Text = text;
        Parameters = parameters ?? new Dictionary<string, object>();
    }

    /// <summary>Gets the query's text.</summary>
    public string Text { get; }

    /// <summary>Gets the query's parameters.</summary>
    public IDictionary<string, object> Parameters { get; }

    /// <summary>Print the query.</summary>
    /// <returns>A string representation of the query.</returns>
    public override string ToString()
    {
        return $"`{Text}`, {Parameters.ToContentString()}";
    }
}
