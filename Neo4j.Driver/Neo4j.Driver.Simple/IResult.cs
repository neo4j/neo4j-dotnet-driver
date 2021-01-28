// Copyright (c) "Neo4j"
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

using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver
{
    /// <summary>
    /// Provides access to the result as an <see cref="IEnumerable{T}"/> of <see cref="IRecord"/>s.
    /// The records in the result is lazily retrieved and could only be visited once.
    /// </summary>
    /// <remarks> Calling <see cref="Enumerable.ToList{TSource}"/> will enumerate the entire stream.</remarks>
    public interface IResult : IEnumerable<IRecord>
    {
        /// <summary>
        /// Gets the keys in the result.
        /// </summary>
        IReadOnlyList<string> Keys { get; }

        /// <summary>
        /// Investigate the next upcoming record without changing the current position in the result.
        /// </summary>
        /// <returns>The next record, or null if there is no next record.</returns>
        IRecord Peek();

        /// <summary>
        /// Consume the entire result, yielding a summary of it.
        /// Calling this method exhausts the result.
        /// If you want to obtain the summary without discarding the records,
        /// use <see cref="Enumerable.ToList{TSource}"/> to buffer all unconsumed records into memory instead.
        /// </summary>
        /// <returns>A summary for running the query.</returns>
        /// <remarks>This method could be called multiple times.
        /// If all records in the records stream are already consumed, then this method will return the summary directly.</remarks>
        IResultSummary Consume();
    }
}