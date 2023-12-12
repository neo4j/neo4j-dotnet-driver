namespace Neo4j.Driver;

/// <summary>
/// The provided token has expired. The current driver instance is considered invalid. It should not be used
/// anymore. The client must create a new driver instance with a valid token.
/// </summary>
public class TokenExpiredException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.TokenExpired";

    /// <summary>Create a new <see cref="TokenExpiredException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public TokenExpiredException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsTokenExpiredError(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}