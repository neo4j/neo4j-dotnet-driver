using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// There was an error that points us to a fatal problem for routing table discovery, like the requested database
/// could not be found. This kind of errors are identified as non-transient and are not retried.
/// </summary>
[DataContract]
public class FatalDiscoveryException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Database.DatabaseNotFound";

    /// <summary>Create a new <see cref="FatalDiscoveryException"/> with an error code and an error message.</summary>
    /// <param name="message">The error message.</param>
    public FatalDiscoveryException(string message)
        : base(ErrorCode, message)
    {
    }

    internal static bool IsFatalDiscoveryError(string code)
    {
        return code.Equals(ErrorCode);
    }
}