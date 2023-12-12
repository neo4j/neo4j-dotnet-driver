using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A <see cref="DatabaseException"/> indicates that there is a problem within the underlying database. The error
/// code provided can be used to determine further detail for the problem.
/// </summary>
[DataContract]
public class DatabaseException : Neo4jException
{
    /// <summary>Create a new <see cref="DatabaseException"/>.</summary>
    public DatabaseException()
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error error message.</summary>
    /// <param name="message">The error message.</param>
    public DatabaseException(string message) : base(string.Empty, message)
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public DatabaseException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="DatabaseException"/> with an error code, an error message and an exception.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception which caused this error.</param>
    public DatabaseException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}