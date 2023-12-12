using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The exception that is thrown when calling <see cref="IAsyncTransaction.CommitAsync"/> or
/// <see cref="IAsyncTransaction.RollbackAsync"/> on an <see cref="IAsyncTransaction"/> that has already been closed.
/// </summary>
[DataContract]
public class TransactionClosedException : ClientException
{
    /// <summary>Create a new <see cref="TransactionClosedException"/> with an error message.</summary>
    /// <param name="message">The error message</param>
    public TransactionClosedException(string message) : base(message)
    {
    }
}