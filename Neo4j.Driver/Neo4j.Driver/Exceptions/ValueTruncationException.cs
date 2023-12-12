using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// A value retrieved from the database needs to be truncated for this conversion to work, and will cause working
/// with a modified data.
/// </summary>
[DataContract]
public class ValueTruncationException : ClientException
{
    /// <summary>Create a new <see cref="ValueTruncationException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public ValueTruncationException(string message) : base(message)
    {
    }
}