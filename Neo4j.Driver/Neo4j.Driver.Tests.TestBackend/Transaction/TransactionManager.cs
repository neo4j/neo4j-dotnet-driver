using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionWrapper
{
    private readonly Func<IResultCursor, Task<string>> ResultHandler;

    public TransactionWrapper(IAsyncTransaction transaction, Func<IResultCursor, Task<string>> resultHandler)
    {
        Transaction = transaction;
        ResultHandler = resultHandler;
    }

    public IAsyncTransaction Transaction { get; }

    public async Task<string> ProcessResults(IResultCursor cursor)
    {
        return await ResultHandler(cursor);
    }
}

internal class TransactionManager
{
    private Dictionary<string, TransactionWrapper> Transactions { get; } = new();

    public string AddTransaction(TransactionWrapper transation)
    {
        var key = ProtocolObjectManager.GenerateUniqueIdString();
        Transactions.Add(key, transation);
        return key;
    }

    public void RemoveTransaction(string key)
    {
        Transactions.Remove(key);
    }

    public TransactionWrapper FindTransaction(string key)
    {
        return Transactions[key];
    }
}
