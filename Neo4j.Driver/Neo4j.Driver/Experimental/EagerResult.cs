// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
/// Complete result from a cypher query.
/// </summary>
public sealed class EagerResult : IReadOnlyList<IRecord>
{
    public static implicit operator (IRecord[] Records, IResultSummary Summary)(EagerResult result)
    {
        return (result.Records, result.Summary);
    }
    
    public static implicit operator (IRecord[] Records, IResultSummary Summary, string[] Keys)(EagerResult result)
    {
        return (result.Records, result.Summary, result.Keys);
    }
    
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Least common set of fields in <see cref="Records"/>.
    /// </summary>
    public string[] Keys { get; init; }

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// All Records from query.
    /// </summary>
    public IRecord[] Records { get; init; }

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Query summary.
    /// </summary>
    public IResultSummary Summary { get; init; }

    /// <inheritdoc />
    public IEnumerator<IRecord> GetEnumerator()
    {
        return ((IEnumerable<IRecord>)Records).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return Records.GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => Records.Length;

    /// <inheritdoc />
    public IRecord this[int index] => Records[index];
}
