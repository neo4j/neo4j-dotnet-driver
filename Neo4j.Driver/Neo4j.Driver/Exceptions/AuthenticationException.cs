using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// Failed to authentication the client to the server due to bad credentials To recover from this error, close the
/// current driver and restart with the correct credentials
/// </summary>
[DataContract]
public class AuthenticationException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.Unauthorized";

    /// <summary>Create a new <see cref="AuthenticationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AuthenticationException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsAuthenticationError(string code)
    {
        return code.Equals(ErrorCode);
    }
}