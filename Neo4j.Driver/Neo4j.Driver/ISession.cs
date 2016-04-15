// Copyright (c) 2002-2016 "Neo Technology,"
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

namespace Neo4j.Driver
{

    /// <summary>
    /// A live session with a Neo4j instance.
    ///
    /// Sessions serve two purposes. For one, they are an optimization. By keeping state on the database side, we can
    /// avoid re-transmitting certain metadata over and over.
    ///
    /// Sessions also serve a role in transaction isolation and ordering semantics. Neo4j requires
    /// "sticky sessions", meaning all requests within one session must always go to the same Neo4j instance.
    ///
    /// Session objects are not thread safe, if you want to run concurrent operations against the database,
    /// simply create multiple sessions objects.
    /// </summary>
    public interface ISession : IStatementRunner
    {
        /// <summary>
        /// Begin a new transaction in this session. A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IStatementRunner.Run(Statement)"/>
        /// method directly on this session interface as well. When you use that method, your statement automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple statements in the same transaction, you should wrap them in a transaction using this
        /// method.
        ///
        /// </summary>
        /// <returns>A new transaction.</returns>
        ITransaction BeginTransaction();
    }

    /// <summary>
    ///  Common interface for components that can execute Neo4j statements.
    /// </summary>
    /// <remarks>
    /// <see cref="ISession"/> and <see cref="ITransaction"/>
    /// </remarks>
    public interface IStatementRunner : IDisposable
    {
        /// <summary>
        /// 
        /// Run a statement and return a result stream.
        ///
        /// This method takes a set of parameters that will be injected into the
        /// statement by Neo4j. Using parameters is highly encouraged, it helps avoid
        /// dangerous cypher injection attacks and improves database performance as
        /// Neo4j can re-use query plans more often.
        ///
        /// </summary>
        /// <param name="statement">A Neo4j statement.</param>
        /// <param name="parameters">Input data for the statement.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(string statement, IDictionary<string, object> parameters = null);

        /// <summary>
        ///
        /// Run a statement and return a result stream.
        ///
        /// </summary>
        /// <param name="statement">A Neo4j statement, <see cref="Statement"/>.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(Statement statement);

        /// <summary>
        /// Run a statement and return a result stream.
        /// </summary>
        /// <param name="statement">A Neo4j statement.</param>
        /// <param name="parameters">A parameter dictonary which is made of prop.Name=prop.Value pairs would be created.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(string statement, object parameters);
    }

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
        void Success();

        /// <summary>
        /// Mark this transaction as failed.When you call <see cref="IDisposable.Dispose"/>, the transaction will be rolled back.
        ///
        /// After this method has been called, there is nothing that can be done to "un-mark" it .This is a safety feature
        /// to make sure no other code calls <see cref="Success"/> and makes a transaction commit that was meant to be rolled
        /// back.
        /// </summary>
        void Failure();
    }
}