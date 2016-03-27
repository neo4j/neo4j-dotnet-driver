//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver
{
    /// <summary>
    /// Provides access to the result as an <see cref="IEnumerable{T}"/> of <see cref="IRecord"/>s.
    /// The records in the result is lazily retrived and could only be visited once.
    /// </summary>
    /// <remarks> Calling <see cref="Enumerable.ToList{TSource}"/> will enumerate the entire stream.</remarks>
    public interface IStatementResult : IEnumerable<IRecord>, IDisposable
    {
        /// <summary>
        /// Gets the keys in the result
        /// </summary>
        IReadOnlyList<string> Keys { get; }
        /// <summary>
        /// Gets the <see cref="IResultSummary"/> after streaming the whole records to the client. 
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this is called before all the records have been visited.</exception>
        IResultSummary Summary { get; }
        /// <summary>
        /// Return the first record in the result, failing if there is not exactly one record, or if this result has already been used to move past the first record.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if not exactly one result have been found, or if this result has already been used to move past the first record.</exception>
        /// <returns>The single record in the result.</returns>
        IRecord Single();
        /// <summary>
        /// Investigate the next upcoming record without changing the current position in the result.
        /// </summary>
        /// <returns>The next record, or null if there is no next record</returns>
        IRecord Peek();
        /// <summary>
        /// Consume the entire result, yielding a summary of it.
        /// Calling this method exhausts the result.
        /// </summary>
        /// <returns>A summary for running the statement</returns>
        /// <remarks>This method could be called multiple times. If no more record could be consumed then calling this method has the same effect of calling <see cref="IStatementResult.Summary"/>.</remarks>
        IResultSummary Consume();
    }
}