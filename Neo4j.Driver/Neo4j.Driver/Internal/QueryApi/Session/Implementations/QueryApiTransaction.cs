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

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiTransaction : IInternalAsyncTransaction
{
    private readonly ITransactionCommitter _committer;
    private readonly IDisposable _loggingContext;
    private readonly ILogger _logger;
    private readonly ITransactionRollback _rollback;
    private readonly ITransactionRunner _runner;

    public QueryApiTransaction(
        ITransactionRunner runner,
        ITransactionCommitter committer,
        ITransactionRollback rollback,
        ILoggingContextTracker contextTracker,
        QueryApiTransactionContext transactionContext,
        ILogger logger)
    {
        _runner = runner;
        _committer = committer;
        _rollback = rollback;
        _loggingContext = contextTracker.Add("tx", transactionContext);
        _logger = logger;
    }

    public TransactionConfig TransactionConfig => TransactionConfig.Default;

    public bool IsOpen { get; private set; } = true;

    public bool IsErrored(out Exception ex)
    {
        // any error would have already been thrown
        ex = null!;
        return false;
    }

    public async Task CommitAsync()
    {
        EnsureOpen();
        _logger.Debug("Committing transaction");
        await _committer.CommitAsync().ConfigureAwait(false);
        IsOpen = false;
    }

    public async Task RollbackAsync()
    {
        EnsureOpen();
        _logger.Debug("Rolling back transaction");
        await _rollback.RollbackAsync().ConfigureAwait(false);
        IsOpen = false;
    }

    public Task<IResultCursor> RunAsync(string query) => RunAsync(new Query(query));

    public Task<IResultCursor> RunAsync(string query, object parameters) =>
        RunAsync(new Query(query, parameters));

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters) =>
        RunAsync(new Query(query, parameters));

    public Task<IResultCursor> RunAsync(Query query)
    {
        EnsureOpen();
        return _runner.RunAsync(query);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsOpen)
        {
            IsOpen = false;
            _logger.Debug("Disposing open transaction — rolling back");
            await _rollback.RollbackAsync().ConfigureAwait(false);
        }

        _loggingContext.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new TransactionClosedException("Transaction has already been committed or rolled back.");
        }
    }
}
