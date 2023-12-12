using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The result has already been consumed either by explicit consume call, or by termination of session or
/// transaction where the result was obtained. Once a result is consumed, the records in the result is not accessible
/// anymore.
/// </summary>
[DataContract]
public class ResultConsumedException : ClientException
{
    /// <summary>Create a new <see cref="ResultConsumedException"/> with an error message</summary>
    /// <param name="message">The error message</param>
    public ResultConsumedException(string message) : base(message)
    {
    }
}