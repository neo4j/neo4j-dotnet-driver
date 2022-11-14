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

namespace Neo4j.Driver;

/// <summary>
/// A live session with a Neo4j instance. Sessions serve a role in transaction isolation and ordering semantics.
/// Within a session, transactions run sequentially one after another. Session objects are not thread safe, if you want to
/// run concurrent operations against the database, simply create multiple session objects.
/// </summary>
public interface IAsyncSession : IAsyncQueryRunner
{
    /// <summary>
    /// Gets the bookmark received following the last successfully completed <see cref="IAsyncTransaction" />. If no
    /// bookmark was received or if this transaction was rolled back, the bookmark value will not be changed.
    /// </summary>
    [Obsolete("Replaced by LastBookmarks. Will be removed in 6.0")]
    Bookmark LastBookmark { get; }

    /// <summary>
    /// Gets the bookmark received following the last successfully completed <see cref="IAsyncTransaction" />. If no
    /// bookmark was received or if this transaction was rolled back, the bookmark value will not be changed.
    /// </summary>
    Bookmarks LastBookmarks { get; }

    /// <summary>Gets the session configurations back</summary>
    SessionConfig SessionConfig { get; }

    /// <summary>
    /// Asynchronously begin a new transaction in this session using server default transaction configurations. A
    /// session can have at most one transaction running at a time, if you want to run multiple concurrent transactions, you
    /// should use multiple concurrent sessions. All data operations in Neo4j are transactional. However, for convenience we
    /// provide a <see cref="IAsyncQueryRunner.RunAsync(Query)" /> method directly on this session interface as well. When you
    /// use that method, your query automatically gets wrapped in a transaction. If you want to run multiple queries in the
    /// same transaction, you should wrap them in a transaction using this method.
    /// </summary>
    /// <returns>A task of a new transaction.</returns>
    Task<IAsyncTransaction> BeginTransactionAsync();

    /// <summary>
    /// Asynchronously begin a new transaction with a specific <see cref="TransactionConfig" /> in this session. A
    /// session can have at most one transaction running at a time, if you want to run multiple concurrent transactions, you
    /// should use multiple concurrent sessions. All data operations in Neo4j are transactional. However, for convenience we
    /// provide a <see cref="IAsyncQueryRunner.RunAsync(Query)" /> method directly on this session interface as well. When you
    /// use that method, your query automatically gets wrapped in a transaction. If you want to run multiple queries in the
    /// same transaction, you should wrap them in a transaction using this method.
    /// </summary>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task of a new transaction.</returns>
    Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read" /> transaction with a specific
    /// <see cref="TransactionConfig" />.
    /// </summary>
    /// <typeparam name="T">The return type of the given unit of work.</typeparam>
    /// <param name="work">The <see cref="Func{ITransactionAsync, T}" /> to be applied to a new read transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task of a result as returned by the given unit of work.</returns>
    [Obsolete("Deprecated, Use ExecuteReadAsync. Will be removed in 6.0.")]
    Task<T> ReadTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read" /> transaction with a specific
    /// <see cref="TransactionConfig" />.
    /// </summary>
    /// <param name="work">The <see cref="Func{ITransactionAsync, Task}" /> to be applied to a new read transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task representing the completion of the transactional read operation enclosing the given unit of work.</returns>
    [Obsolete("Deprecated, Use ExecuteReadAsync. Will be removed in 6.0.")]
    Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work in a <see cref="AccessMode.Write" /> transaction with a specific
    /// <see cref="TransactionConfig" />.
    /// </summary>
    /// <typeparam name="T">The return type of the given unit of work.</typeparam>
    /// <param name="work">The <see cref="Func{ITransactionAsync, T}" /> to be applied to a new write transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task of a result as returned by the given unit of work.</returns>
    [Obsolete("Deprecated, Use ExecuteWriteAsync. Will be removed in 6.0.")]
    Task<T> WriteTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work in a <see cref="AccessMode.Write" /> transaction with a specific
    /// <see cref="TransactionConfig" />.
    /// </summary>
    /// <param name="work">The <see cref="Func{ITransactionAsync, Task}" /> to be applied to a new write transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task representing the completion of the transactional write operation enclosing the given unit of work.</returns>
    [Obsolete("Deprecated, Use ExecuteWriteAsync. Will be removed in 6.0.")]
    Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig" />.</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}" /> to be applied to a new read transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task<TResult> ExecuteReadAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder> action = null);

    /// <summary>Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig" />.</summary>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}" /> to be applied to a new read transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ExecuteReadAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig" />.</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}" /> to be applied to a new write transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task<TResult> ExecuteWriteAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder> action = null);

    /// <summary>Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig" />.</summary>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}" /> to be applied to a new write transaction.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction. This configuration overrides server side default transaction configurations. See
    /// <see cref="TransactionConfig" />
    /// </param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Close all resources used in this Session. If any transaction is left open in this session without commit or
    /// rollback, then this method will rollback the transaction.
    /// </summary>
    /// <returns>A task representing the completion of successfully closed the session.</returns>
    Task CloseAsync();

    /// <summary>
    /// Asynchronously run a query with the specific <see cref="TransactionConfig" /> and return a task of result
    /// stream. This method accepts a String representing a Cypher query which will be compiled into a query object that can be
    /// used to efficiently execute this query multiple times. This method optionally accepts a set of parameters which will be
    /// injected into the query object query by Neo4j.
    /// </summary>
    /// <param name="query">A Cypher query.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction.
    /// </param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously run a query with the customized <see cref="TransactionConfig" /> and return a task of result
    /// stream. This method accepts a String representing a Cypher query which will be compiled into a query object that can be
    /// used to efficiently execute this query multiple times. This method optionally accepts a set of parameters which will be
    /// injected into the query object query by Neo4j.
    /// </summary>
    /// <param name="query">A Cypher query.</param>
    /// <param name="parameters">Input parameters for the query.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction.
    /// </param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    Task<IResultCursor> RunAsync(
        string query,
        IDictionary<string, object> parameters,
        Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute a query with the specific <see cref="TransactionConfig" /> and return a task of result
    /// stream.
    /// </summary>
    /// <param name="query">A Cypher query, <see cref="Query" />.</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder" />, defines how to set the configurations for the new
    /// transaction.
    /// </param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action = null);
}
