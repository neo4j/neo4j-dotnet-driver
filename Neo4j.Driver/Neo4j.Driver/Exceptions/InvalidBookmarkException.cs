namespace Neo4j.Driver;

/// <summary>The provided bookmark is invalid. To recover from this a new session needs to be created.</summary>
public class InvalidBookmarkException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Transaction.InvalidBookmark";

    /// <summary>Create a new <see cref="InvalidBookmarkException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidBookmarkException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsInvalidBookmarkException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}