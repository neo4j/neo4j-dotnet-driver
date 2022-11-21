// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver;

/// <summary>
/// A reactive session, which provides the same functionality as <see cref="IAsyncSession"/> but with reactive
/// API.
/// </summary>
public interface IRxSession : IRxRunnable
{
    /// <summary>
    /// Returns the bookmark received following the last successfully completed query, which is executed either in an
    /// <see cref="IRxTransaction"/> obtained from this session instance or directly through one of the <strong>Run</strong>
    /// overrides of this session instance. If no bookmark was received or if this transaction was rolled back, the bookmark
    /// value will not be changed.
    /// </summary>
    [Obsolete("Replaced with LastBookmarks. Will be removed in 6.0.")]
    Bookmark LastBookmark { get; }

    /// <summary>
    /// Returns the bookmark received following the last successfully completed query, which is executed either in an
    /// <see cref="IRxTransaction"/> obtained from this session instance or directly through one of the <strong>Run</strong>
    /// overrides of this session instance. If no bookmark was received or if this transaction was rolled back, the bookmark
    /// value will not be changed.
    /// </summary>
    Bookmarks LastBookmarks { get; }

    /// <summary>Gets the session configuration</summary>
    SessionConfig SessionConfig { get; }

    /// <summary>
    /// Begin a new <strong>explicit</strong> <see cref="IRxTransaction"/>. Actual transaction is only created once an
    /// observer is subscribed to the returned reactive stream. A session instance can only have at most one transaction at a
    /// time. If you want to run multiple concurrent transactions, you should use multiple concurrent sessions.
    /// </summary>
    /// <returns>a reactive stream which will generate at most one <see cref="IRxTransaction"/> instance.</returns>
    IObservable<IRxTransaction> BeginTransaction();

    /// <summary>
    /// Begin a new <strong>explicit</strong> <see cref="IRxTransaction"/> with the provided
    /// <see cref="TransactionConfig"/>. Actual transaction is only created once an observer is subscribed to the returned
    /// reactive stream. A session instance can only have at most one transaction at a time. If you want to run multiple
    /// concurrent transactions, you should use multiple concurrent sessions.
    /// </summary>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// returned transaction
    /// </param>
    /// <returns>a reactive stream which will generate at most one <see cref="IRxTransaction"/> instance.</returns>
    IObservable<IRxTransaction> BeginTransaction(Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Create <see cref="IRxResult">a reactive result</see> that will execute the query with the provided
    /// <see cref="TransactionConfig"/> that applies to the underlying auto-commit transaction.
    /// </summary>
    /// 
    /// <param name="query">Query to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// returned transaction
    /// </param>
    /// <returns>a reactive result</returns>
    /// 
    /// <see cref="Run(string,System.Action{TransactionConfigBuilder})"></see>
    IRxResult Run(string query, Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Create <see cref="IRxResult">a reactive result</see> that will execute the query  with the specified
    /// parameters and the provided <see cref="TransactionConfig"/>  that applies to the  underlying auto-commit transaction.
    /// </summary>
    /// 
    /// <param name="query">Query to be executed</param>
    /// <param name="parameters">
    /// a parameter dictionary, can be an <see cref="IDictionary{TKey,TValue}"/> or an anonymous
    /// object
    /// </param>
    /// <param name="action">configuration for the auto-commit transaction</param>
    /// <returns>a reactive result</returns>
    /// 
    /// <see cref="Run(string,System.Action{TransactionConfigBuilder})"></see>
    IRxResult Run(string query, object parameters, Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Create <see cref="IRxResult">a reactive result</see> that will execute the given query with the provided
    /// <see cref="TransactionConfig"/> that applies to the underlying auto-commit transaction. The query is only executed when
    /// an <see cref="IObserver{T}"/> is subscribed to one of the reactive streams that can be accessed through the returned
    /// reactive result.
    /// </summary>
    /// <param name="query">Query to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// auto-commit transaction
    /// </param>
    /// <returns>a reactive result</returns>
    IRxResult Run(Query query, Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Read">Read</see>
    /// <see cref="IRxTransaction">reactive transaction</see>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    [Obsolete("Deprecated, Replaced by ExecuteRead, will be removed in 6.0")]
    IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Read">Read</see>
    /// <see cref="IRxTransaction">reactive transaction</see> which is created with the provided
    /// <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// created transaction
    /// </param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    [Obsolete("Deprecated, Replaced by ExecuteRead, will be removed in 6.0")]
    IObservable<T> ReadTransaction<T>(
        Func<IRxTransaction, IObservable<T>> work,
        Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Write">Read</see>
    /// <see cref="IRxTransaction">reactive transaction</see>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    [Obsolete("Deprecated, Replaced by ExecuteWrite, will be removed in 6.0")]
    IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Write">Read</see>
    /// <see cref="IRxTransaction">reactive transaction</see> which is created with the provided
    /// <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// created transaction
    /// </param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    [Obsolete("Deprecated, Replaced by ExecuteWrite, will be removed in 6.0")]
    IObservable<T> WriteTransaction<T>(
        Func<IRxTransaction, IObservable<T>> work,
        Action<TransactionConfigBuilder> action);

    /// <summary>Execute the provided unit of work in a <see cref="AccessMode.Read"/> managed reactive transaction.</summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    IObservable<T> ExecuteRead<T>(Func<IRxRunnable, IObservable<T>> work);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Read"/> managed reactive transaction which is
    /// created with the provided <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// created transaction
    /// </param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    IObservable<T> ExecuteRead<T>(
        Func<IRxRunnable, IObservable<T>> work,
        Action<TransactionConfigBuilder> action);

    /// <summary>Execute the provided unit of work in a <see cref="AccessMode.Write"/> managed reactive transaction.</summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    IObservable<T> ExecuteWrite<T>(Func<IRxRunnable, IObservable<T>> work);

    /// <summary>
    /// Execute the provided unit of work in a <see cref="AccessMode.Write"/> managed reactive transaction which is
    /// created with the provided <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">a unit of work to be executed</param>
    /// <param name="action">
    /// Given a <see cref="TransactionConfigBuilder"/>, defines how to create the configuration for the
    /// created transaction
    /// </param>
    /// <typeparam name="T">the return type of the unit of work</typeparam>
    /// <returns>the reactive stream returned by the unit of work</returns>
    IObservable<T> ExecuteWrite<T>(
        Func<IRxRunnable, IObservable<T>> work,
        Action<TransactionConfigBuilder> action);

    /// <summary>
    /// Closes this session and returns an empty reactive stream. The type parameter makes it easier to chain this
    /// method to other reactive streams.
    /// </summary>
    /// <typeparam name="T">the desired return type</typeparam>
    /// <returns>an empty reactive stream</returns>
    IObservable<T> Close<T>();
}
