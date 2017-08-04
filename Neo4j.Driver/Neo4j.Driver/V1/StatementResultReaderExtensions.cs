// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
    public static class StatementResultReaderExtensions
    {
        /// <summary>
        /// Return the only record in the result stream.
        /// </summary>
        /// <param name="result">The result stream</param>
        /// <returns>The only record in the result stream.</returns>
        /// <remarks>Throws <exception cref="InvalidOperationException"></exception>
        /// if the result contains more than one record or the result is empty.</remarks>
        public static async Task<IRecord> SingleAsync(this IStatementResultReader result)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            if (await result.ReadAsync().ConfigureAwait(false))
            {
                var record = result.Current();
                if (!await result.ReadAsync().ConfigureAwait(false))
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
        public static async Task<IList<IRecord>> ToListAsync(this IStatementResultReader result)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            IList<IRecord> list = new List<IRecord>();
            while (await result.ReadAsync().ConfigureAwait(false))
            {
                list.Add(result.Current());
            }
            return list;
        }

        /// <summary>
        /// Read each record in the result stream and aplly the operation on each record.
        /// </summary>
        /// <param name="result">The result stream.</param>
        /// <param name="operation">The operation is carried out on each record.</param>
        /// <returns>A Task that completes when all records have been processed.</returns>
        public static async Task ForEachAsync(this IStatementResultReader result, Action<IRecord> operation)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            while (await result.ReadAsync().ConfigureAwait(false))
            {
                var record = result.Current();
                operation(record);
            }
        }

        /// <summary>
        /// Read each record in the result stream and aplly operations on each record
        /// </summary>
        /// <param name="result">The result stream.</param>
        /// <param name="operation">The operation is carried out on each record.</param>
        /// <returns>A Task that completes whe nall records have been processed.</returns>
        public static async Task ForEachAsync(this IStatementResultReader result, Func<IRecord, Task> operation)
        {
            Throw.ArgumentNullException.IfNull(result, nameof(result));
            while (await result.ReadAsync().ConfigureAwait(false))
            {
                var record = result.Current();
                await operation(record).ConfigureAwait(false);
            }
        }
    }
}
