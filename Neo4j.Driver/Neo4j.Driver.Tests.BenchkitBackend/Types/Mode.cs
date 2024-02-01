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

namespace Neo4j.Driver.Tests.BenchkitBackend.Types;

/// <summary>
/// Defines the parallel/sequential mode in which the queries should be executed.
/// </summary>
public enum Mode
{
    /// <summary>
    /// Execute the queries sequentially in a single transaction.
    /// </summary>
    SequentialQueries,

    /// <summary>
    /// Execute each query in a separate transaction sequentially in the same session.
    /// </summary>
    SequentialTransactions,

    /// <summary>
    /// Execute each query in a separate session sequentially.
    /// </summary>
    SequentialSessions,

    /// <summary>
    /// Execute the queries in parallel in q session per query.
    /// </summary>
    ParallelSessions
}
