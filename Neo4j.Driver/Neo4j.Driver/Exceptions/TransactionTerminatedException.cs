using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The exception that is thrown when trying to further interact with a terminated transaction.
/// Transactions are terminated when they incur errors. <br/>
/// If created by the driver the <see cref="Neo4jException.Code"/> will be null.
/// </summary>
[DataContract]
public sealed class TransactionTerminatedException : ClientException
{
    public override bool IsRetriable => (InnerException as Neo4jException)?.IsRetriable ?? false;

    internal TransactionTerminatedException(Exception inner) :
        base((inner as Neo4jException)?.Code, inner.Message, inner)
    {
    }
}
