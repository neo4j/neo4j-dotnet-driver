using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="SessionExpiredException"/> indicates that the session can no longer satisfy the criteria under
/// which it was acquired, e.g. a server no longer accepts write requests. A new session needs to be acquired from the
/// driver and all actions taken on the expired session must be replayed.
/// </summary>
[DataContract]
public class SessionExpiredException : Neo4jException
{
    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public SessionExpiredException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsRetriable => true;
}