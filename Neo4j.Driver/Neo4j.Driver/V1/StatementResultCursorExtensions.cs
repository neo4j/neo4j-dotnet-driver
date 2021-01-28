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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Extension methods for <see cref="IStatementResultCursor"/>
    /// </summary>
    public static class StatementResultCursorExtensions
    {
        /// <summary>
        /// Return the only record in the result stream.
        /// </summary>
        /// <param name="result">The result stream</param>
        /// <returns>The only record in the result stream.</returns>
        /// <remarks>Throws <exception cref="InvalidOperationException"></exception>
        /// if the result contains more than one record or the result is empty.</remarks>
        public static async Task<IRecord> SingleAsync(this IStatementResultCursor result)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            if (await result.FetchAsync().ConfigureAwait(false))
            {
                var record = result.Current;
                if (!await result.FetchAsync().ConfigureAwait(false))
                {
                    return record;
                }
                else
                {
                    throw new InvalidOperationException( "The result contains more than one element." );
                }
            }
            else
            {
                throw new InvalidOperationException("The result is empty.");
            }
        }

        /// <summary>
        /// Pull all records in the result stream into memory and return in a list.
        /// </summary>
        /// <param name="result"> The result stream.</param>
        /// <returns>A list with all records in the result stream.</returns>
        public static async Task<List<IRecord>> ToListAsync(this IStatementResultCursor result)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            List<IRecord> list = new List<IRecord>();
            while (await result.FetchAsync().ConfigureAwait(false))
            {
                list.Add(result.Current);
            }
            return list;
        }

        /// <summary>
        /// Apply the operation on each record in the result stream and return the operation results in a list.
        /// </summary>
        /// <typeparam name="T">The return type of the list</typeparam>
        /// <param name="result">The result stream.</param>
        /// <param name="operation">The operation to carry out on each record.</param>
        /// <returns>A list of collected operation result.</returns>
        public static async Task<List<T>> ToListAsync<T>(this IStatementResultCursor result, Func<IRecord, T> operation)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            var list = new List<T>();
            while (await result.FetchAsync().ConfigureAwait(false))
            {
                var record = result.Current;
                list.Add(operation(record));
            }
            return list;
        }

        /// <summary>
        /// Read each record in the result stream and apply the operation on each record.
        /// </summary>
        /// <param name="result">The result stream.</param>
        /// <param name="operation">The operation is carried out on each record.</param>
        /// <returns>The result summary after all records have been processed.</returns>
        public static async Task<IResultSummary> ForEachAsync(this IStatementResultCursor result, Action<IRecord> operation)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            while (await result.FetchAsync().ConfigureAwait(false))
            {
                var record = result.Current;
                operation(record);
            }
            return await result.SummaryAsync().ConfigureAwait(false);
        }
    }
}
