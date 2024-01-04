using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The exception that is thrown when calling an operation in the driver which uses a server feature that is not
/// available on the connected server version.
/// </summary>
[DataContract]
public class UnsupportedFeatureException : ClientException
{
    /// <inheritdoc />
    public override bool IsRetriable => false;
    
    /// <summary>
    /// Creates a new <see cref="UnsupportedFeatureException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message</param>
    internal UnsupportedFeatureException(string message) : base(message)
    {
    }
}
