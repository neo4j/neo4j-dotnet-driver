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
    /// session.BeginTransaction(new TransactionConfig());
    /// </code>
    /// </summary>
    public sealed class TransactionConfig : IEquatable<TransactionConfig>
    {
        internal static readonly TransactionConfig Empty = new TransactionConfig();
        private IDictionary<string, object> _metadata = PackStream.EmptyDictionary;
        private TimeSpan _timeout = TimeSpan.Zero;

        /// <summary>
        /// Get and set transaction timeout.
        /// Transactions that execute longer than the configured timeout will be terminated by the database.
        /// This functionality allows to limit query/transaction execution time.
        /// Specified timeout overrides the default timeout configured in the database using <code>dbms.transaction.timeout</code> setting.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the value given to transaction timeout in milliseconds is less or equal to zero</exception>
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

                _timeout = value;
            }
        }

        /// <summary>
        /// Get and set the transaction metadata.
        /// Specified metadata will be attached to the executing transaction and visible in the output of <code>dbms.listQueries</code>
        /// and <code>dbms.listTransactions</code> procedures. It will also get logged to the <code>query.log</code>.
        /// Transactions starting with this <see cref="TransactionConfig"/>
        /// This functionality makes it easier to tag transactions and is equivalent to <code>dbms.setTXMetaData</code> procedure.
        /// Leave this field unmodified to use default timeout configured on database.
        /// </summary>
        public IDictionary<string, object> Metadata
        {
            get => _metadata;
            set => _metadata =
                value ?? throw new ArgumentNullException(nameof(value), "Transaction metadata should not be null");
        }
        
        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="TransactionConfig"/> and
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TransactionConfig config && Equals(config);
        }
        
        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="TransactionConfig" /> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(TransactionConfig other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Timeout == other.Timeout &&
                   (Metadata == other.Metadata ||
                    Metadata.Count == other.Metadata.Count && !Metadata.Except(other.Metadata).Any());
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            var hashCode = Timeout.GetHashCode();
            hashCode = (hashCode * 397) ^ (Metadata != null ? Metadata.GetHashCode() : 0);
            return hashCode;
        }

        /// <summary>
        /// Returns the config content in a nice string representation.
        /// </summary>
        /// <returns>The content of the transaction config in a string.</returns>
        public override string ToString()
        {
            return $"{GetType().Name}{{{nameof(Metadata)}={Metadata.ToContentString()}, {nameof(Timeout)}={Timeout}}}";
        }
        
        /// <summary>
        /// Test if the config is empty. Empty configuration will not be sent to server when starting a transaction,
        /// and therefore the transactions will be started using default transaction configuration values set in server configurations. 
        /// </summary>
        /// <returns>True if the transaction config is empty, otherwise false.</returns>
        internal bool IsEmpty()
        {
            return Equals(Empty) || _timeout <= TimeSpan.Zero && (_metadata == null || _metadata.Count == 0);
        }
    }
}