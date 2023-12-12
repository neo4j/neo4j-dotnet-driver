using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="TransientException"/> signals a failed operation that may be able to succeed if this operation is
/// retried without any intervention by application-level functionality. The error code provided can be used to determine
/// further details for the problem.
/// </summary>
[DataContract]
public class TransientException : Neo4jException
{
    /// <summary>Create a new <see cref="TransientException"/>.</summary>
    public TransientException()
    {
    }

    /// <summary>Create a new <see cref="TransientException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public TransientException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="TransientException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception which caused this error.</param>
    public TransientException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsRetriable => true;
}