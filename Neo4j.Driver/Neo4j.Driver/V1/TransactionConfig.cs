// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Configuration object containing settings for explicit and auto-commit transactions.
    /// Instances are immutable and can be reused for multiple transactions.
    /// </summary>
    public class TransactionConfig
    {
        internal static readonly TransactionConfig Empty = new TransactionConfig();
        private IDictionary<string, object> _metadata = PackStream.EmptyDictionary;
        private TimeSpan _timeout = TimeSpan.Zero;

        /// <summary>
        /// Get and set transaction timeout.
        /// Transactions that execute longer than the configured timeout will be terminated by the database.
        /// This functionality allows to limit query/transaction execution time.
        /// Specified timeout overrides the default timeout configured in the database using <code>dbms.transaction.timeout</code> setting. 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the value given to transaction timeout is less or equal to zero,
        /// or greater than <see cref="long.MaxValue"/></exception>
        public TimeSpan Timeout
        {
            get => _timeout;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Transaction timeout should not be zero or negative.");
                }
                if (value.TotalMilliseconds > long.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        $"Transaction timeout in milliseconds must be less than or equal to long.MaxValue ({long.MaxValue}).");
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Get and set the transaction metadata.
        /// Specified metadata will be attached to the executing transaction and visible in the output of <code>dbms.listQueries</code>
        /// and <code>dbms.listTransactions</code> procedures. It will also get logged to the <code>query.log</code>.
        /// Transactions starting with this <see cref="TransactionConfig"/>
        /// This functionality makes it easier to tag transactions and is equivalent to <code>dbms.setTXMetaData</code> procedure.
        /// </summary>
        public IDictionary<string, object> Metadata
        {
            get => _metadata;
            set => _metadata =
                value ?? throw new ArgumentNullException(nameof(value), "Transaction metadata should not be null");
        }

        internal bool IsEmpty()
        {
            return _timeout <= TimeSpan.Zero && (_metadata == null || _metadata.Count == 0);
        }
    }
}