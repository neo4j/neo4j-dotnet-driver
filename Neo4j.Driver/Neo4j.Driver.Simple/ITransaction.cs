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

namespace Neo4j.Driver
{
    /// <summary>
    /// Represents a transaction in the Neo4j database.
    ///
    /// This interface may seem surprising in that it does not have explicit <c>Commit</c> or <c>Rollback</c> methods.
    /// It is designed to minimize the complexity of the code you need to write to use transactions in a safe way, ensuring
    /// that transactions are properly rolled back even if there is an exception while the transaction is running.
    /// </summary>
    public interface ITransaction : IStatementRunner
    {
        /// <summary>
        /// Mark this transaction as successful. You must call this method before calling <see cref="IDisposable.Dispose"/> to have your
        /// transaction committed.
        /// </summary>
        void Commit();

        /// <summary>
        /// Mark this transaction as failed. Calling <see cref="IDisposable.Dispose"/> will roll back the transaction.
        ///
        /// Marking a transaction as failed is irreversible and guarantees that subsequent calls to <see cref="Commit"/> will not change it's status.
        /// </summary>
        void Rollback();
    }
}