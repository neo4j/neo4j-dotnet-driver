using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>An unknown security error occurred.</summary>
[DataContract]
public class UnknownSecurityException : SecurityException
{
    private const string ErrorCodePrefix = "Neo.ClientError.Security.";

    /// <summary>Create a new <see cref="UnknownSecurityException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    public UnknownSecurityException(string message, string code) : base($"{ErrorCodePrefix}*", message)
    {
        Code = code;
    }

    internal static bool IsUnknownSecurityException(string code)
    {
        return code.StartsWith(ErrorCodePrefix);
    }
}