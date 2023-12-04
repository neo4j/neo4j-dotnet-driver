// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
