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
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver
{
    /// <summary>
    ///     The result of running a statement, a stream of records represented as a cursor.
    ///     The result cursor can be used to iterate over all the records in the stream and provide access
    ///     to their content.
    /// 
    ///     Initially, before <see cref="Next"/> has been called at least once, all record values are null.
    /// 
    ///     Calling <see cref="Enumerable.ToList{TSource}"/> will enumerate the entire stream.
    /// </summary>
    public interface IResultCursor : IResultRecordAccessor, IDisposable
    {
        /// <summary>
        /// Provides access to the results as an <see cref="IEnumerable{T}"/> of <see cref="IRecord"/>s. 
        /// </summary>
        /// <remarks> Calling <see cref="Enumerable.ToList{TSource}"/> will enumerate the entire stream.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IRecord"/>s.</returns>
        IEnumerable<Record> Stream();
        /// <summary>
        /// Gets the <see cref="IResultSummary"/> after streaming the whole records to the client. 
        /// </summary>
        /// <exception cref="ClientException">Thrown if this is called before all the records have been streamed (if <see cref="AtEnd"/> is <c>false</c>)</exception>
        IResultSummary Summary { get; }
        /// <summary>
        /// Gets the position in the stream.
        /// </summary>
        long Position { get; }
        /// <summary>
        /// Attempts to move the cursor to next record in the stream.
        /// </summary>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        bool Next();
        /// <summary>
        /// Gets whether or not the stream is at the end. 
        /// </summary>
        bool AtEnd { get; }
    }

    /// <summary>
    /// Provides more methods on <see cref="IResultCursor"/>
    /// </summary>
    public interface IExtendedResultCursor : IResultCursor, IResources
    {
        /// <summary>
        /// Advance the cursor as if calling next multiple times.
        /// </summary>
        /// <param name="records">The amount of the records to be skipped</param>
        /// <returns>the actual number of records successfully skipped</returns>
        long Skip(long records);
        /// <summary>
        /// Limit this cursor to return no more than the given number of records after the current record.
        /// As soon as the described amount of records have been returned, all further records are discarded.
        /// Calling limit again before the described amount of records have been returned, replaces the limit (overwriting the previous limit).
        /// </summary>
        /// <param name="records">The maximum number of records to return from future calls to <see cref="IResultCursor.Next()"/></param>
        /// <returns>The actual position of the last record to be returned</returns>
        long Limit(long records);
        /// <summary>
        /// Move the cursor to the first record in the stream.
        /// </summary>
        /// <returns><c>true</c> if successfully moved to the first record in the stream, otherwise, <c>false</c></returns>
        bool First();
        /// <summary>
        /// Move to the first record.
        /// </summary>
        /// <returns><c>true</c> if successfully moved to the first record and no more records left in the stream, otherwise, <c>false</c></returns>
        bool Single();
        /// <summary>
        /// Investigate the next upcoming record without changing the position of this cursor.
        /// </summary>
        /// <returns>the next record, or null if there is no next record</returns>
        Record Peek();
    }

    /// <summary>
    /// Represents resources that could be open and closed.
    /// </summary>
    public interface IResources 
    {
        /// <summary>
        /// Test if the resources are open
        /// </summary>
        /// <returns><c>true</c> if the resources are open</returns>
        bool IsOpen();
        /// <summary>
        /// Close the Resources and make them not accessable.
        /// </summary>
        void Close();
    }
}