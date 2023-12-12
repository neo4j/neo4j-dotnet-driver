using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="ConnectionReadTimeoutException"/> indicates that the driver timed out trying to read from the
/// network socket.
/// </summary>
[DataContract]
public class ConnectionReadTimeoutException : Neo4jException
{
    /// <summary>Create a new <see cref="ConnectionReadTimeoutException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ConnectionReadTimeoutException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ConnectionReadTimeoutException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionReadTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsRetriable => true;
}