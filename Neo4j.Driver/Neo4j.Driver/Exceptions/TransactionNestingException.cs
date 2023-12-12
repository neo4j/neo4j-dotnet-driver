using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// An attempt to BeginTransaction has been made before the sessions existing transaction has been consumed or
/// rolled back. e.g. An attempt to nest transactions has occurred. A session can only have a single transaction at a time.
/// </summary>
[DataContract]
public class TransactionNestingException : ClientException
{
    /// <summary>Create a new <see cref="TransactionNestingException"/> with an error message</summary>
    /// <param name="message">The error message</param>
    public TransactionNestingException(string message) : base(message)
    {
    }
}