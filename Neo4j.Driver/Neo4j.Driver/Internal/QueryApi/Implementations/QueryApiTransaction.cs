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

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiTransaction : IAsyncTransaction
{
    private readonly ICommitTransactionHandler _commitHandler;
    private readonly IQueryApiResultCursorBuilder _cursorBuilder;
    private readonly IRollbackTransactionHandler _rollbackHandler;
    private readonly IRunInTransactionHandler _runHandler;
    private bool _closed;

    public QueryApiTransaction(
        IRunInTransactionHandler runHandler,
        ICommitTransactionHandler commitHandler,
        IRollbackTransactionHandler rollbackHandler,
        IQueryApiResultCursorBuilder cursorBuilder)
    {
        _runHandler = runHandler;
        _commitHandler = commitHandler;
        _rollbackHandler = rollbackHandler;
        _cursorBuilder = cursorBuilder;
    }

    public TransactionConfig TransactionConfig => TransactionConfig.Default;

    public async Task CommitAsync()
    {
        EnsureOpen();
        _closed = true;
        await _commitHandler.CommitTransactionAsync().ConfigureAwait(false);
    }

    public async Task RollbackAsync()
    {
        EnsureOpen();
        _closed = true;
        await _rollbackHandler.RollbackTransactionAsync().ConfigureAwait(false);
    }

    public Task<IResultCursor> RunAsync(string query) => RunAsync(new Query(query));

    public Task<IResultCursor> RunAsync(string query, object parameters) =>
        RunAsync(new Query(query, parameters));

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters) =>
        RunAsync(new Query(query, parameters));

    public async Task<IResultCursor> RunAsync(Query query)
    {
        EnsureOpen();
        var response = await _runHandler.RunInTransactionAsync(query).ConfigureAwait(false);
        return _cursorBuilder.Build(response, query);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_closed)
        {
            _closed = true;
            await _rollbackHandler.RollbackTransactionAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void EnsureOpen()
    {
        if (_closed)
            throw new TransactionClosedException("Transaction has already been committed or rolled back.");
    }
}
