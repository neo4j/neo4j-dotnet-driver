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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiTransaction : IScopedTransaction
{
    private readonly ITransactionBeginner _beginner;
    private readonly ITransactionCommitter _committer;
    private readonly ILogger _logger;
    private readonly ITransactionRollback _rollback;
    private readonly ITransactionRunner _runner;

    private bool _disposed;

    public QueryApiTransaction(
        ITransactionBeginner beginner,
        ITransactionRunner runner,
        ITransactionCommitter committer,
        ITransactionRollback rollback,
        ILogger logger)
    {
        _beginner = beginner;
        _runner = runner;
        _committer = committer;
        _rollback = rollback;
        _logger = logger;
    }

    public event AsyncEventHandler? Disposed;

    public TransactionConfig TransactionConfig => TransactionConfig.Default;

    public bool IsOpen { get; private set; }

    public bool IsErrored(out Exception ex)
    {
        // any error would have already been thrown
        ex = null!;
        return false;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        await _beginner.BeginAsync(cancellationToken).ConfigureAwait(false);
        IsOpen = true;
    }

    public async Task CommitAsync()
    {
        EnsureOpen();
        _logger.LogDebug("Committing transaction");
        await _committer.CommitAsync().ConfigureAwait(false);
        IsOpen = false;
    }

    public async Task RollbackAsync()
    {
        EnsureOpen();
        _logger.LogDebug("Rolling back transaction");
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (IsOpen)
        {
            IsOpen = false;
            _logger.LogDebug("Disposing open transaction — rolling back");
            await _rollback.RollbackAsync().ConfigureAwait(false);
        }

        await Disposed.FireAsync().ConfigureAwait(false);
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
