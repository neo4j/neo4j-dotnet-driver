using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>A generic argument error has occurred. To recover from this a new session needs to be created.</summary>
[DataContract]
public class ArgumentErrorException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Statement.ArgumentError";

    /// <summary>Create a new <see cref="ArgumentErrorException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ArgumentErrorException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsArgumentErrorException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}