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
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    /// <summary>
    /// A live session with a Neo4j instance.
    ///
    /// Sessions serve a role in transaction isolation and ordering semantics.
    /// Within a session, transactions run sequentially one after another.
    ///
    /// Session objects are not thread safe, if you want to run concurrent operations against the database,
    /// simply create multiple session objects.
    /// </summary>
    public interface IAsyncSession : IAsyncStatementRunner
    {
        /// <summary>
        /// Asynchronously begin a new transaction in this session using server default transaction configurations.
        /// A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IAsyncStatementRunner.RunAsync(Statement)"/>
        /// method directly on this session interface as well. When you use that method, your statement automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple statements in the same transaction, you should wrap them in a transaction using this
        /// method.
        /// </summary>
        /// <returns>A task of a new transaction.</returns>
        Task<IAsyncTransaction> BeginTransactionAsync();

        /// <summary>
        /// Asynchronously begin a new transaction with a specific <see cref="TransactionOptions"/> in this session.
        /// A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IAsyncStatementRunner.RunAsync(Statement)"/>
        /// method directly on this session interface as well. When you use that method, your statement automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple statements in the same transaction, you should wrap them in a transaction using this
        /// method.
        /// </summary>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionOptions"/></param>
        /// <returns>A task of a new transaction.</returns>
        Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionOptions> optionsBuilder);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new read transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new read transaction.</param>
        /// <returns>A task representing the completion of the transactional read operation enclosing the given unit of work.</returns>
        Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction with a specific <see cref="TransactionOptions"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new read transaction.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionOptions"/></param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionOptions> optionsBuilder);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction with a specific <see cref="TransactionOptions"/>.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new read transaction.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionOptions"/></param>
        /// <returns>A task representing the completion of the transactional read operation enclosing the given unit of work.</returns>
        Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionOptions> optionsBuilder);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new write transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new write transaction.</param>
        /// <returns>A task representing the completion of the transactional write operation enclosing the given unit of work.</returns>
        Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction with a specific <see cref="TransactionOptions"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new write transaction.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionOptions"/></param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work, Action<TransactionOptions> optionsBuilder);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction with a specific <see cref="TransactionOptions"/>.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new write transaction.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// This configuration overrides server side default transaction configurations. See <see cref="TransactionOptions"/></param>
        /// <returns>A task representing the completion of the transactional write operation enclosing the given unit of work.</returns>
        Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionOptions> optionsBuilder);

        /// <summary>
        /// Close all resources used in this Session. If any transaction is left open in this session without commit or rollback,
        /// then this method will rollback the transaction.
        /// </summary>
        /// <returns>A task representing the completion of successfully closed the session.</returns>
        Task CloseAsync();

        /// <summary>
        /// Gets the bookmark received following the last successfully completed <see cref="IAsyncTransaction"/>.
        /// If no bookmark was received or if this transaction was rolled back, the bookmark value will not be changed.
        /// </summary>
        Bookmark LastBookmark { get; }

        /// <summary>
        /// 
        /// Asynchronously run a statement with the specific <see cref="TransactionOptions"/> and return a task of result stream.
        ///
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object statement by Neo4j. 
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement, Action<TransactionOptions> optionsBuilder);

        /// <summary>
        /// 
        /// Asynchronously run a statement with the customized <see cref="TransactionOptions"/> and return a task of result stream.
        ///
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object statement by Neo4j. 
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">Input parameters for the statement.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement, IDictionary<string, object> parameters,
            Action<TransactionOptions> optionsBuilder);

        /// <summary>
        ///
        /// Asynchronously execute a statement with the specific <see cref="TransactionOptions"/> and return a task of result stream.
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement, <see cref="Statement"/>.</param>
        /// <param name="optionsBuilder">Given a <see cref="TransactionOptions"/>, defines how to set the configurations for the new transaction.
        /// </param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(Statement statement, Action<TransactionOptions> optionsBuilder);
    }
}