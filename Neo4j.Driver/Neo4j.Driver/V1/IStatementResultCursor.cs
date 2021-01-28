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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Provides access to the result as an asynchronous stream of <see cref="IRecord"/>s.
    /// The records in the result is lazily retrieved and could only be visited once in a sequential order.
    /// </summary>
    /// <remarks> Calling <see cref="Enumerable.ToList{TSource}"/> will enumerate the entire stream.</remarks>
    public interface IStatementResultCursor
    {
        /// <summary>
        /// Gets the keys in the result.
        /// </summary>
        IReadOnlyList<string> Keys { get; }

        /// <summary>
        /// Asynchronously gets the <see cref="IResultSummary"/> after streaming the whole records to the client.
        /// If the records in the result are not fully consumed,
        /// then calling this method will force to pull all remaining records into buffer to yield the summary.
        ///
        /// If you want to obtain the summary but discard the records, use <see cref="ConsumeAsync"/> instead.
        ///
        /// If all records in the records stream are already consumed, then this method will return the summary directly.
        /// </summary>
        /// <returns>A task returning the result summary of the running statement.</returns>
        Task<IResultSummary> SummaryAsync();

        /// <summary>
        /// Asynchronously investigates the next upcoming record without changing the current position in the result.
        /// </summary>
        /// <returns>A task returning next record or null if there is no next record.</returns>
        Task<IRecord> PeekAsync();

        /// <summary>
        /// Asynchronously consume the entire result, yielding a summary of it.
        /// Upon completion of the returned task, the result will be exhausted.
        /// </summary>
        /// <returns>A task returning the summary for running the statement.</returns>
        /// <remarks>This method could be called multiple times. If no more record could be consumed then calling this method has the same effect of calling <see cref="IStatementResultCursor.SummaryAsync()"/>.</remarks>
        Task<IResultSummary> ConsumeAsync();

        /// <summary>
        /// Asynchronously tries to advance to the next record.
        /// </summary>
        /// <returns>A task returning a <see cref="bool"/>. Task's result is true if there is any result to be consumed, false otherwise.</returns>
        Task<bool> FetchAsync();

        /// <summary>
        /// Returns the current record that has already been read via <see cref="FetchAsync"/>.
        /// </summary>
        /// <value>A <see cref="IRecord"/> holding the data sent by the server.</value>
        /// <remarks>Throws <exception cref="InvalidOperationException"></exception>
        /// if accessed without calling <see cref="FetchAsync"/> or <see cref="PeekAsync"/>.</remarks>
        IRecord Current { get; }
    }
}
