using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// There was a bolt protocol violation of the contract between the driver and the server. When seen this error,
/// contact driver developers.
/// </summary>
[DataContract]
public class ProtocolException : Neo4jException
{
    private const string ErrorCodeInvalid = "Neo.ClientError.Request.Invalid";
    private const string ErrorCodeInvalidFormat = "Neo.ClientError.Request.InvalidFormat";

    /// <summary>Create a new <see cref="ProtocolException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ProtocolException(string message) : base(message)
    {
    }

    /// <summary>Create a new <see cref="ProtocolException"/> with an error code and an error message.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public ProtocolException(string code, string message) : base(code, message)
    {
    }

    /// <summary>Create a new <see cref="SessionExpiredException"/> with an error message and an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }

    internal static bool IsProtocolError(string code)
    {
        return code.Equals(ErrorCodeInvalid) || code.Equals(ErrorCodeInvalidFormat);
    }
}