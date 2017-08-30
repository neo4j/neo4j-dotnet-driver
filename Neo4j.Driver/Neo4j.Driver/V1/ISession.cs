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

namespace Neo4j.Driver.V1
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
    /// simply create multiple session objects.
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

        /// <summary>
        /// This method is deprecated.
        /// </summary>
        /// <param name="bookmark"></param>
        /// <returns></returns>
        [Obsolete("Pass in bookmark at session instead.")]
        ITransaction BeginTransaction(string bookmark);

        /// <summary>
        /// Asynchronously begin a new transaction in this session. A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IStatementRunner.RunAsync(Statement)"/>
        /// method directly on this session interface as well. When you use that method, your statement automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple statements in the same transaction, you should wrap them in a transaction using this
        /// method.
        ///
        /// </summary>
        /// <returns>A task of a new transaction.</returns>
        Task<ITransaction> BeginTransactionAsync();
        
        /// <summary>
        /// Execute given unit of work in a  <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new read transaction.</param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T ReadTransaction<T>(Func<ITransaction, T> work);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new read transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> ReadTransactionAsync<T>(Func<ITransaction, Task<T>> work);

        /// <summary>
        /// Execute given unit of work in a  <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Action{T}"/> to be applied to a new read transaction.</param>
        void ReadTransaction(Action<ITransaction> work);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new read transaction.</param>
        /// <returns>A task representing the completion of the transactional read operation enclosing the given unit of work.</returns>
        Task ReadTransactionAsync(Func<ITransaction, Task> work);
        
        /// <summary>
        ///  Execute given unit of work in a  <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{TResult}"/> to be applied to a new write transaction.</param>
        /// <returns>A result as returned by the given unit of work.</returns>
        T WriteTransaction<T>(Func<ITransaction, T> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new write transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> WriteTransactionAsync<T>(Func<ITransaction, Task<T>> work);

        /// <summary>
        ///  Execute given unit of work in a  <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Action{T}"/> to be applied to a new write transaction.</param>
        void WriteTransaction(Action<ITransaction> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new write transaction.</param>
        /// <returns>A task representing the completion of the transactional write operation enclosing the given unit of work.</returns>
        Task WriteTransactionAsync(Func<ITransaction, Task> work);

        /// <summary>
        /// Close all resources used in this Session. If any transaction is left open in this session without commit or rollback,
        /// then this method will rollback the transaction.
        /// </summary>
        /// <returns>A task representing the completion of successfully closed the session.</returns>
        Task CloseAsync();

        /// <summary>
        /// Gets the bookmark received following the last successfully completed <see cref="ITransaction"/>.
        /// If no bookmark was received or if this transaction was rolled back, the bookmark value will not be changed.
        /// </summary>
        string LastBookmark { get; }
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
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. 
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(string statement);

        /// <summary>
        /// 
        /// Asynchronously run a statement and return a task of result stream.
        ///
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object statement by Neo4j. 
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement);

        /// <summary>
        /// Execute a statement and return a result stream.
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">A parameter dictonary which is made of prop.Name=prop.Value pairs would be created.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(string statement, object parameters);
        
        /// <summary>
        /// Asynchronously execute a statement and return a task of result stream.
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">A parameter dictonary which is made of prop.Name=prop.Value pairs would be created.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement, object parameters);

        /// <summary>
        /// 
        /// Run a statement and return a result stream.
        ///
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object statement by Neo4j. 
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">Input parameters for the statement.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(string statement, IDictionary<string, object> parameters);

        /// <summary>
        /// 
        /// Asynchronously run a statement and return a task of result stream.
        ///
        /// This method accepts a String representing a Cypher statement which will be 
        /// compiled into a query object that can be used to efficiently execute this
        /// statement multiple times. This method optionally accepts a set of parameters
        /// which will be injected into the query object statement by Neo4j. 
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">Input parameters for the statement.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement, IDictionary<string, object> parameters);

        /// <summary>
        ///
        /// Execute a statement and return a result stream.
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement, <see cref="Statement"/>.</param>
        /// <returns>A stream of result values and associated metadata.</returns>
        IStatementResult Run(Statement statement);

        /// <summary>
        ///
        /// Asynchronously execute a statement and return a task of result stream.
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement, <see cref="Statement"/>.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(Statement statement);

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
        /// Mark this transaction as failed. Calling <see cref="IDisposable.Dispose"/> will roll back the transaction.
        ///
        /// Marking a transaction as failed is irreversable and guarantees that subsequent calls to <see cref="Success"/> will not change it's status.
        /// </summary>
        void Failure();
        
        /// <summary>
        /// Asynchronously commit this transaction.
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Asynchronously roll back this transaction.
        /// </summary>
        Task RollbackAsync();

    }
}