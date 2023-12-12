using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>An error occurred related to data typing.</summary>
[DataContract]
public class TypeException : ClientException
{
    private const string ErrorCode = "Neo.ClientError.Statement.TypeError";

    /// <summary>Create a new <see cref="TypeException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public TypeException(string message) : base(ErrorCode, message)
    {
    }

    internal static bool IsTypeException(string code)
    {
        return string.Equals(code, ErrorCode);
    }
}