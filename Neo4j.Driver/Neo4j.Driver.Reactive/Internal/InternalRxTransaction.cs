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
using System.Reactive.Linq;

namespace Neo4j.Driver.Internal;

internal class InternalRxTransaction : IRxTransaction
{
    private readonly IInternalAsyncTransaction _transaction;

    public InternalRxTransaction(IInternalAsyncTransaction transaction)
    {
        _transaction = transaction;
    }

    public bool IsOpen => _transaction.IsOpen;
    public TransactionConfig TransactionConfig => _transaction.TransactionConfig;

    public IObservable<T> Commit<T>()
    {
        return Observable.FromAsync(() => _transaction.CommitAsync()).SelectMany(_ => Observable.Empty<T>());
    }

    public IObservable<T> Rollback<T>()
    {
        return Observable.FromAsync(() => _transaction.RollbackAsync()).SelectMany(_ => Observable.Empty<T>());
    }

#region Run Methods

    public IRxResult Run(string query)
    {
        return Run(query, null);
    }

    public IRxResult Run(string query, object parameters)
    {
        return Run(new Query(query, parameters.ToDictionary()));
    }

    public IRxResult Run(Query query)
    {
        return new RxResult(
            Observable.FromAsync(() => _transaction.RunAsync(query))
                .Cast<IInternalResultCursor>());
    }

#endregion
}
