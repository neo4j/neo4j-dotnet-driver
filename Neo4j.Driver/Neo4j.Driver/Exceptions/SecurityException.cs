using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// Failed to connect the driver to the server due to security errors When this type of error happens, recreation
/// of the driver might be required.
/// </summary>
[DataContract]
public class SecurityException : Neo4jException
{
    internal bool Notified = false;
    internal bool Retriable = false;

    /// <summary>Create a new <see cref="SecurityException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public SecurityException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="SecurityException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public SecurityException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="SecurityException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Whether or not the exception is retriable. If the exception is retriable, the driver will try to
    /// re-run the operation that caused the exception.
    /// </summary>
    public override bool IsRetriable => Retriable;
}