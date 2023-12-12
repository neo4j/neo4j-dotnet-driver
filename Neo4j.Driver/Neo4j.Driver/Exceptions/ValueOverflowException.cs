using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A value retrieved from the database cannot be represented with the type to be converted, and will cause
/// working with a modified data.
/// </summary>
[DataContract]
public class ValueOverflowException : ClientException
{
    /// <summary>Create a new <see cref="ValueTruncationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ValueOverflowException(string message) : base(message)
    {
    }
}