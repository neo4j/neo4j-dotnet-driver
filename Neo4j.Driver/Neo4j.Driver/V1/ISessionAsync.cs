using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public interface ISessionAsync : IStatementRunnerAsync
    {
        /// <summary>
        /// Asynchronously begin a new transaction in this session. A session can have at most one transaction running at a time, if you
        /// want to run multiple concurrent transactions, you should use multiple concurrent sessions.
        /// 
        /// All data operations in Neo4j are transactional. However, for convenience we provide a <see cref="IStatementRunnerAsync.RunAsync(Statement)"/>
        /// method directly on this session interface as well. When you use that method, your statement automatically gets
        /// wrapped in a transaction.
        ///
        /// If you want to run multiple statements in the same transaction, you should wrap them in a transaction using this
        /// method.
        ///
        /// </summary>
        /// <returns>A task of a new transaction.</returns>
        Task<ITransactionAsync> BeginTransactionAsync();

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new read transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> ReadTransactionAsync<T>(Func<ITransactionAsync, Task<T>> work);

        /// <summary>
        /// Asynchronously execute given unit of work in a <see cref="AccessMode.Read"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new read transaction.</param>
        /// <returns>A task representing the completion of the transactional read operation enclosing the given unit of work.</returns>
        Task ReadTransactionAsync(Func<ITransactionAsync, Task> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <typeparam name="T">The return type of the given unit of work.</typeparam>
        /// <param name="work">The <see cref="Func{ITransactionAsync, T}"/> to be applied to a new write transaction.</param>
        /// <returns>A task of a result as returned by the given unit of work.</returns>
        Task<T> WriteTransactionAsync<T>(Func<ITransactionAsync, Task<T>> work);

        /// <summary>
        ///  Asynchronously execute given unit of work in a <see cref="AccessMode.Write"/> transaction.
        /// </summary>
        /// <param name="work">The <see cref="Func{ITransactionAsync, Task}"/> to be applied to a new write transaction.</param>
        /// <returns>A task representing the completion of the transactional write operation enclosing the given unit of work.</returns>
        Task WriteTransactionAsync(Func<ITransactionAsync, Task> work);

        /// <summary>
        /// Close all resources used in this Session. If any transaction is left open in this session without commit or rollback,
        /// then this method will rollback the transaction.
        /// </summary>
        /// <returns>A task representing the completion of successfully closed the session.</returns>
        Task CloseAsync();

    }

    /// <summary>
    ///  Interface for components that can asynchronously execute Neo4j statements.
    /// </summary>
    /// <remarks>
    /// <see cref="ISessionAsync"/> and <see cref="ITransactionAsync"/>
    /// </remarks>
    public interface IStatementRunnerAsync
    {
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
        Task<IStatementResultReader> RunAsync(string statement);

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
        /// Asynchronously execute a statement and return a task of result stream.
        ///
        /// </summary>
        /// <param name="statement">A Cypher statement, <see cref="Statement"/>.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(Statement statement);

        /// <summary>
        /// Asynchronously execute a statement and return a task of result stream.
        /// </summary>
        /// <param name="statement">A Cypher statement.</param>
        /// <param name="parameters">A parameter dictonary which is made of prop.Name=prop.Value pairs would be created.</param>
        /// <returns>A task of a stream of result values and associated metadata.</returns>
        Task<IStatementResultCursor> RunAsync(string statement, object parameters);

    }

    /// <summary>
    /// Represents a transaction in the Neo4j database.
    ///
    /// This interface may seem surprising in that it does not have explicit <c>Commit</c> or <c>Rollback</c> methods.
    /// It is designed to minimize the complexity of the code you need to write to use transactions in a safe way, ensuring
    /// that transactions are properly rolled back even if there is an exception while the transaction is running.
    /// </summary>
    public interface ITransactionAsync : IStatementRunnerAsync
    {
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
