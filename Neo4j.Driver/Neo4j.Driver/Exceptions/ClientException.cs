using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="ClientException"/> indicates that the client has carried out an operation incorrectly. The error
/// code provided can be used to determine further detail for the problem.
/// </summary>
[DataContract]
public class ClientException : Neo4jException
{
    /// <summary>Create a new <see cref="ClientException"/>.</summary>
    public ClientException()
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ClientException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public ClientException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Create a new <see cref="ClientException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClientException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}