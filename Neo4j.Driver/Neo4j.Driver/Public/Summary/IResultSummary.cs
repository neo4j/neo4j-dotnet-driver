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
using System.Collections.Generic;

namespace Neo4j.Driver;

/// <summary>
/// The result summary of running a query. The result summary interface can be used to investigate details about
/// the result, like the type of query run, how many and which kinds of updates have been executed, and query plan and
/// profiling information if available. The result summary is only available after all result records have been consumed.
/// Keeping the result summary around does not influence the lifecycle of any associated session and/or transaction.
/// </summary>
public interface IResultSummary
{
    /// <summary>Gets query that has been executed.</summary>
    Query Query { get; }

    /// <summary>Gets statistics counts for the query.</summary>
    ICounters Counters { get; }

    /// <summary>Gets type of query that has been executed.</summary>
    QueryType QueryType { get; }

    /// <summary>
    /// Gets if the result contained a query plan or not, i.e. is the summary of a Cypher <c>PROFILE</c> or
    /// <c>EXPLAIN</c> query.
    /// </summary>
    bool HasPlan { get; }

    /// <summary>
    /// Gets if the result contained profiling information or not, i.e. is the summary of a Cypher <c>PROFILE</c>
    /// query.
    /// </summary>
    bool HasProfile { get; }

    /// <summary>Gets query plan for the executed query if available, otherwise null.</summary>
    /// <remarks>This describes how the database will execute your query.</remarks>
    IPlan Plan { get; }

    /// <summary>Gets profiled query plan for the executed query if available, otherwise null.</summary>
    /// <remarks>
    /// This describes how the database did execute your query. If the query you executed (<see cref="HasProfile"/>
    /// was profiled), the query plan will contain detailed information about what each step of the plan did. That more
    /// in-depth version of the query plan becomes available here.
    /// </remarks>
    IProfiledPlan Profile { get; }

    /// <summary>
    /// Gets a list of notifications produced while executing the query. The list will be empty if no notifications
    /// produced while executing the query.
    /// </summary>
    /// <remarks>
    /// A list of notifications that might arise when executing the query. Notifications can be warnings about
    /// problematic queries or other valuable information that can be presented in a client. Unlike failures or errors,
    /// notifications do not affect the execution of a query.
    /// </remarks>
    IList<INotification> Notifications { get; }
    
    /// <summary>
    /// This is a preview API, This API may change between minor revisions.<br/>
    /// Gets the GQL statuses produced by the server when executing a statement or query.
    /// </summary>
    /// <seealso cref="IGqlStatusObject">For more information about GQL Statuses</seealso>
    /// <since>5.23.0</since>
    IList<IGqlStatusObject> GqlStatusObjects { get; }
    
    /// <summary>
    /// The time it took the server to make the result available for consumption. Default to <c>-00:00:00.001</c> if
    /// the server version does not support this field in summary.
    /// </summary>
    /// <remarks>Field introduced in Neo4j 3.1.</remarks>
    TimeSpan ResultAvailableAfter { get; }

    /// <summary>
    /// The time it took the server to consume the result. Default to <c>-00:00:00.001</c> if the server version does
    /// not support this field in summary.
    /// </summary>
    /// <remarks>Field introduced in Neo4j 3.1.</remarks>
    TimeSpan ResultConsumedAfter { get; }

    /// <summary>Get some basic information of the server where the query is carried out</summary>
    IServerInfo Server { get; }

    /// <summary>Get the database information that this summary is generated from.</summary>
    IDatabaseInfo Database { get; }
}
