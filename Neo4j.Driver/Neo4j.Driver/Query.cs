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

using System.Collections.Generic;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Telemetry;

namespace Neo4j.Driver;

/// <summary>An executable query, i.e. the queries' text and its parameters.</summary>
public class Query
{
    /// <summary>Create a query with no query parameters.</summary>
    /// <param name="text">The query's text</param>
    public Query(string text) : this(text, null)
    {
    }

    /// <summary>Create a query with parameters specified as anonymous objects</summary>
    /// <param name="text">The query's text</param>
    /// <param name="parameters">The query parameters, specified as an object which is then converted into key-value pairs.</param>
    public Query(string text, object parameters)
        : this(text, parameters.ToDictionary())
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

    internal string QueryApiType { get; set; } = QueryApiTypeIdentifier.Unknown;
}
