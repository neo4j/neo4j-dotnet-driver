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
    /// AsyncSession objects are not thread safe, if you want to run concurrent operations against the database,
    /// simply create multiple session objects.
    /// </summary>
    public interface ISession : IQueryRunner
    {
        /// <summary>
        /// Gets the bookmark received following the last successfully completed <see cref="IAsyncTransaction"/>.
        /// If no bookmark was received or if this transaction was rolled back, the bookmark value will not be changed.
        /// </summary>
        Bookmark LastBookmark { get; }


        /// <summary>
        /// Begin a new transaction in this session using server default transaction configurations.
        /// A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        ///
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IQueryRunner.Run(Query)"/>
        /// method directly on this session interface as well. When you use that method, your query automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple queries in the same transaction, you should wrap them in a transaction using this
        /// method.
        ///
        /// </summary>
        /// <returns>A new transaction.</returns>
        ITransaction BeginTransaction();

        /// <summary>
        /// Begin a new transaction with a specific <see cref="TransactionConfig"/> in this session.
        /// A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IQueryRunner.Run(Query)"/>
        /// method directly on this session interface as well. When you use that method, your query automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple queries in the same transaction, you should wrap them in a transaction using this
        /// method.
        ///
        /// </summary>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
        /// <returns>A new transaction.</returns>
        ITransaction BeginTransaction(Action<TransactionConfigBuilder> action);

        /// <summary>
        /// Execute given unit of work in a  <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new read transaction.</param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T ReadTransaction<T>(Func<ITransaction, T> work);

        /// <summary>
        /// Execute given unit of work in a  <see cref="AccessMode.Read"/> transaction with a specific <see cref="TransactionConfig"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new read transaction.</param>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations.</param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T ReadTransaction<T>(Func<ITransaction, T> work, Action<TransactionConfigBuilder> action);

        /// <summary>
        ///  Execute given unit of work in a  <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new write transaction.</param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T WriteTransaction<T>(Func<ITransaction, T> work);

        /// <summary>
        ///  Execute given unit of work in a  <see cref="AccessMode.Write"/> transaction with a specific <see cref="TransactionConfig"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new write transaction.</param>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T WriteTransaction<T>(Func<ITransaction, T> work, Action<TransactionConfigBuilder> action);

        /// <summary>
        /// 
        /// Run a query with the specific <see cref="TransactionConfig"/> and return a result stream.
        ///
        /// This method accepts a String representing a Cypher query which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// query multiple times. 
        /// </summary>
        /// <param name="query">A Cypher query.</param>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(string query, Action<TransactionConfigBuilder> action);

        /// <summary>
        /// 
        /// Run a query with the specific <see cref="TransactionConfig"/> and return a result stream.
        ///
        /// This method accepts a String representing a Cypher query which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// query multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object query by Neo4j. 
        ///
        /// </summary>
        /// <param name="query">A Cypher query.</param>
        /// <param name="parameters">Input parameters for the query.</param>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(string query, IDictionary<string, object> parameters, Action<TransactionConfigBuilder> action);

        /// <summary>
        ///
        /// Execute a query with the specific <see cref="TransactionConfig"/> and return a result stream.
        ///
        /// </summary>
        /// <param name="query">A Cypher query, <see cref="Query"/>.</param>
        /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IResult Run(Query query, Action<TransactionConfigBuilder> action);

        /// <summary>
        /// Gets the session configuration back
        /// </summary>
        SessionConfig SessionConfig { get; }
    }
}