using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend;

internal class TransactionManager
{
    private readonly Dictionary<string, TransactionWrapper> _transactions = new();

    public string AddTransaction(TransactionWrapper transaction)
    {
        var key = ProtocolObjectManager.GenerateUniqueIdString();
        _transactions.Add(key, transaction);
        return key;
    }

    public TransactionWrapper FindTransaction(string key) => _transactions[key];
}