namespace Neo4j.Driver;

/// <summary>The authorization information maintained on the server has expired. The client should reconnect.</summary>
public class AuthorizationException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.AuthorizationExpired";

    /// <summary>Create a new <see cref="AuthorizationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public AuthorizationException(string message) : base(ErrorCode, message)
    {
    }

    /// <inheritdoc/>
    public override bool IsRetriable => true;

    internal static bool IsAuthorizationError(string code)
    {
        return code.Equals(ErrorCode);
    }
}