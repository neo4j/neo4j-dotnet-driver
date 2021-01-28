﻿// Copyright (c) "Neo4j"
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
using System;
using System.Collections.Generic;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// The type of a statement.
    /// </summary>
    public enum StatementType
    {
        /// <summary>
        /// The statement type is unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// The statement is a readonly statement
        /// </summary>
        ReadOnly,
        /// <summary>
        /// The statement is a readwrite statement
        /// </summary>
        ReadWrite,
        /// <summary>
        /// The statement is a writeonly statement
        /// </summary>
        WriteOnly,
        /// <summary>
        /// The statement is a schemawrite statement
        /// </summary>
        SchemaWrite
    }

    /// <summary>
    /// 
    /// The result summary of running a statement. The result summary interface can be used to investigate
    /// details about the result, like the type of query run, how many and which kinds of updates have been executed,
    /// and query plan and profiling information if available.
    ///
    /// The result summary is only available after all result records have been consumed.
    ///
    /// Keeping the result summary around does not influence the lifecycle of any associated session and/or transaction.
    /// </summary>
    public interface IResultSummary
    {
        /// <summary>
        /// Gets statement that has been executed.
        /// </summary>
        Statement Statement { get; }

        /// <summary>
        /// Gets statistics counts for the statement.
        /// </summary>
        ICounters Counters { get; }

        /// <summary>
        /// Gets type of statement that has been executed.
        /// </summary>
        StatementType StatementType { get; }

        /// <summary>
        /// Gets if the result contained a statement plan or not, i.e. is the summary of a Cypher <c>PROFILE</c> or <c>EXPLAIN</c> statement.
        /// </summary>
        bool HasPlan { get; }

        /// <summary>
        /// Gets if the result contained profiling information or not, i.e. is the summary of a Cypher <c>PROFILE</c> statement.
        /// </summary>
        bool HasProfile { get; }

        /// <summary>
        /// Gets statement plan for the executed statement if available, otherwise null.
        /// </summary>
        /// <remarks>
        /// This describes how the database will execute your statement.
        /// </remarks>
        IPlan Plan { get; }

        /// <summary>
        /// Gets profiled statement plan for the executed statement if available, otherwise null.
        /// </summary>
        /// <remarks>
        /// This describes how the database did execute your statement.
        /// 
        /// If the statement you executed (<see cref="HasProfile"/> was profiled), the statement plan will contain detailed
        /// information about what each step of the plan did. That more in-depth version of the statement plan becomes
        /// available here.
        /// 
        /// </remarks>
        IProfiledPlan Profile { get; }

        /// <summary>
        /// Gets a list of notifications produced while executing the statement. The list will be empty if no
        /// notifications produced while executing the statement.
        /// </summary>
        /// <remarks>
        /// A list of notifications that might arise when executing the statement.
        /// Notifications can be warnings about problematic statements or other valuable information that can be presented
        /// in a client.
        /// 
        /// Unlike failures or errors, notifications do not affect the execution of a statement.
        /// 
        /// </remarks>
        IList<INotification> Notifications { get; }

        /// <summary>
        /// The time it took the server to make the result available for consumption.
        /// Default to <c>-00:00:00.001</c> if the server version does not support this field in summary.
        /// </summary>
        /// <remarks>
        /// Field introduced in Neo4j 3.1.
        /// </remarks>
        TimeSpan ResultAvailableAfter { get; }

        /// <summary>
        /// The time it took the server to consume the result.
        /// Default to <c>-00:00:00.001</c> if the server version does not support this field in summary.
        /// </summary>
        /// <remarks>
        /// Field introduced in Neo4j 3.1.
        /// </remarks>
        TimeSpan ResultConsumedAfter { get; }

        /// <summary>
        /// Get some basic information of the server where the statement is carried out
        /// </summary>
        IServerInfo Server { get; }
    }
}
