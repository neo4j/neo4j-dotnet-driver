// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver
{
    /// <summary>
    /// Configuration object containing settings for explicit and auto-commit transactions.
    /// Leave the fields unmodified to use server side transaction configurations.
    /// <para/>
    /// For example, the following code starts a transaction using server default transaction configurations.
    /// <code>
    /// session.BeginTransaction(o=>{});
    /// </code>
    /// </summary>
    public sealed class TransactionOptions
    {
        internal static readonly TransactionOptions Empty = new TransactionOptions();
        private IDictionary<string, object> _metadata;
        private TimeSpan _timeout;

        internal TransactionOptions()
        {
            _timeout = TimeSpan.Zero;
            _metadata = PackStream.EmptyDictionary;
        }

        /// <summary>
        /// Transaction timeout.
        /// Transactions that execute longer than the configured timeout will be terminated by the database.
        /// This functionality allows to limit query/transaction execution time.
        /// Specified timeout overrides the default timeout configured in the database using <code>dbms.transaction.timeout</code> setting.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the value given to transaction timeout in milliseconds is less or equal to zero</exception>
        public TimeSpan Timeout
        {
            get => _timeout;
            internal set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Transaction timeout should not be zero or negative.");
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// The transaction metadata.
        /// Specified metadata will be attached to the executing transaction and visible in the output of <code>dbms.listQueries</code>
        /// and <code>dbms.listTransactions</code> procedures. It will also get logged to the <code>query.log</code>.
        /// Transactions starting with this <see cref="TransactionOptions"/>
        /// This functionality makes it easier to tag transactions and is equivalent to <code>dbms.setTXMetaData</code> procedure.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        public IDictionary<string, object> Metadata
        {
            get => _metadata;
            internal set => _metadata =
                value ?? throw new ArgumentNullException(nameof(value), "Transaction metadata should not be null");
        }

        /// <summary>
        /// Sets the transaction timeout.
        /// Transactions that execute longer than the configured timeout will be terminated by the database.
        /// This functionality allows to limit query/transaction execution time.
        /// Specified timeout overrides the default timeout configured in the database using <code>dbms.transaction.timeout</code> setting.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the value given to transaction timeout in milliseconds is less or equal to zero</exception>
        /// <param name="timeout">the new timeout</param>
        /// <returns>this <see cref="TransactionOptions"/> instance</returns>
        public TransactionOptions WithTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        /// <summary>
        /// The transaction metadata.
        /// Specified metadata will be attached to the executing transaction and visible in the output of <code>dbms.listQueries</code>
        /// and <code>dbms.listTransactions</code> procedures. It will also get logged to the <code>query.log</code>.
        /// Transactions starting with this <see cref="TransactionOptions"/>
        /// This functionality makes it easier to tag transactions and is equivalent to <code>dbms.setTXMetaData</code> procedure.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        /// <param name="metadata">the metadata to set on transaction</param>
        /// <returns>this <see cref="TransactionOptions"/> instance</returns>
        public TransactionOptions WithMetadata(IDictionary<string, object> metadata)
        {
            Metadata = metadata;
            return this;
        }

        /// <summary>
        /// Returns the config content in a nice string representation.
        /// </summary>
        /// <returns>The content of the transaction config in a string.</returns>
        public override string ToString()
        {
            return $"{GetType().Name}{{{nameof(Metadata)}={Metadata.ToContentString()}, {nameof(Timeout)}={Timeout}}}";
        }
    }
}