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

namespace Neo4j.Driver;

/// <summary>Complete, materialized result from a cypher query.</summary>
/// <typeparam name="T">The type of the value that will be in the <see cref="Result"/> property.</typeparam>
public sealed class EagerResult<T>
{
    internal EagerResult(T result, IResultSummary summary, string[] keys)
    {
        Result = result;
        Summary = summary;
        Keys = keys;
    }

    /// <summary>Least common set of fields in <see cref="Result"/>.</summary>
    public string[] Keys { get; init; }

    /// <summary>The materialized result of the query.</summary>
    public T Result { get; init; }

    /// <summary>Query summary.</summary>
    public IResultSummary Summary { get; init; }

    /// <summary>Deconstructs the result into its constituent parts.</summary>
    /// <param name="result">The result returned from the query.</param>
    /// <param name="summary">The summary of the result.</param>
    /// <param name="keys">The keys present in the result.</param>
    public void Deconstruct(out T result, out IResultSummary summary, out string[] keys)
    {
        keys = Keys;
        result = Result;
        summary = Summary;
    }

    /// <summary>Deconstructs the result into its constituent parts.</summary>
    /// <param name="result">The result returned from the query.</param>
    /// <param name="summary">The summary of the result.</param>
    public void Deconstruct(out T result, out IResultSummary summary)
    {
        result = Result;
        summary = Summary;
    }
}
