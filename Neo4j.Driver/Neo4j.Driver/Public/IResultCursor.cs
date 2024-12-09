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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Provides access to the result as an asynchronous stream of <see cref="IRecord"/>s. The records in the result
/// is lazily retrieved and can be visited only once in a sequential order.
/// </summary>
/// <remarks> Calling <see cref="ResultCursorExtensions.ToListAsync(IResultCursor, CancellationToken)"/> will enumerate the entire stream.</remarks>
public interface IResultCursor : IAsyncEnumerable<IRecord>
{
    /// <summary>Returns the current record that has already been read via <see cref="FetchAsync"/>.</summary>
    /// <value>A <see cref="IRecord"/> holding the data sent by the server.</value>
    /// <remarks>
    /// Throws <exception cref="InvalidOperationException"></exception> if accessed without calling
    /// <see cref="FetchAsync"/> or <see cref="PeekAsync"/>.
    /// </remarks>
    IRecord Current { get; }

    /// <summary>
    /// Get whether the cursor is open to read records, a cursor will be considered open if <see cref="ConsumeAsync"/>
    /// has not been called.<br/> Attempting to read records from a closed cursor will throw
    /// <see cref="ResultConsumedException"/>.<br/> Cursors can also be closed if its session is disposed or its session runs a
    /// query.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>Gets the keys in the result.</summary>
    /// <returns>A task of an array of keys.</returns>
    Task<string[]> KeysAsync();

    /// <summary>
    /// Asynchronously gets the <see cref="IResultSummary"/> after streaming the whole records to the client. If the
    /// records in the result are not fully consumed, then calling this method will discard all remaining records to yield the
    /// summary. If you want to obtain the summary without discarding the records, consider buffering the unconsumed result
    /// using <see cref="ResultCursorExtensions.ToListAsync(IResultCursor, CancellationToken)"/>. If all records in the records stream are already
    /// consumed, then this method will return the summary directly.
    /// </summary>
    /// <returns>A task returning the result summary of the running query.</returns>
    Task<IResultSummary> ConsumeAsync();

    /// <summary>Asynchronously investigates the next upcoming record without changing the current position in the result.</summary>
    /// <returns>A task returning next record or null if there is no next record.</returns>
    Task<IRecord> PeekAsync();

    /// <summary>Asynchronously tries to advance to the next record.</summary>
    /// <returns>
    /// A task returning a <see cref="bool"/>. Task's result is true if there is any result to be consumed, false
    /// otherwise.
    /// </returns>
    Task<bool> FetchAsync();
}
