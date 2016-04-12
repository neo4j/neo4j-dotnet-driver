using System;

namespace Neo4j.Driver
{
    /// <summary>
    ///     The Driver instance maintains the connections with the Neo4j database, providing an access point via the
    ///     <see cref="Session" /> method.
    /// </summary>
    /// <remarks>
    ///     The Driver maintains a session pool buffering the <see cref="ISession" />s created by the user. The size of the
    ///     buffer can be
    ///     configured by the <see cref="Config.MaxIdleSessionPoolSize" /> property on the <see cref="Config" /> when creating the
    ///     Driver.
    /// </remarks>
    public interface IDriver : IDisposable
    {
        /// <summary>
        ///     Gets the <see cref="Uri" /> of the Neo4j database.
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        ///     Establish a session with Neo4j instance
        /// </summary>
        /// <returns>
        ///     An <see cref="ISession" /> that could be used to <see cref="IStatementRunner.Run(Statement)" /> a statement or begin a
        ///     transaction
        /// </returns>
        ISession Session();
    }
}