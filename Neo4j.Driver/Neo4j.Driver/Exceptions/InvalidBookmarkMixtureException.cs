namespace Neo4j.Driver;

/// <summary>The provided bookmark is invalid. To recover from this a new session needs to be created.</summary>
public class InvalidBookmarkMixtureException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Transaction.InvalidBookmarkMixture";

    /// <summary>Create a new <see cref="InvalidBookmarkMixtureException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidBookmarkMixtureException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsInvalidBookmarkMixtureException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}