using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>This operation is forbidden.</summary>
[DataContract]
public class ForbiddenException : SecurityException
{
    private const string ErrorCode = "Neo.ClientError.Security.Forbidden";

    /// <summary>Create a new <see cref="ForbiddenException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ForbiddenException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsForbiddenException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}