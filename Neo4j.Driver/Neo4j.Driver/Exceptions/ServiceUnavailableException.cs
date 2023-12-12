using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>A <see cref="ServiceUnavailableException"/> indicates that the driver cannot communicate with the cluster.</summary>
[DataContract]
public class ServiceUnavailableException : Neo4jException
{
    /// <summary>Create a new <see cref="ServiceUnavailableException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ServiceUnavailableException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ServiceUnavailableException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsRetriable => true;
}