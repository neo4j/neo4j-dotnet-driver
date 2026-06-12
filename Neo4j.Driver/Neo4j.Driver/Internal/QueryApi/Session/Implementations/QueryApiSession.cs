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
internal class QueryApiSession : IInternalAsyncSession
{
    private readonly IAutoCommitRunner _autoCommitRunner;
    private readonly IBookmarkTracker _bookmarkTracker;
    private readonly ILogger _logger;
    private readonly IAsyncRetryLogic _retryLogic;
    private readonly IQueryApiTransactionFactory _transactionFactory;

    private bool _closed;
    private IInternalAsyncTransaction? _openTransaction;

    public QueryApiSession(
        SessionConfig sessionConfig,
        IAutoCommitRunner autoCommitRunner,
        IQueryApiTransactionFactory transactionFactory,
        IAsyncRetryLogic retryLogic,
        IBookmarkTracker bookmarkTracker,
        ILogger logger)
    {
        SessionConfig = sessionConfig;
        _autoCommitRunner = autoCommitRunner;
        _transactionFactory = transactionFactory;
        _retryLogic = retryLogic;
        _bookmarkTracker = bookmarkTracker;
        _logger = logger;
    }

    public Bookmarks LastBookmarks => _bookmarkTracker.CurrentBookmarks;

    public SessionConfig SessionConfig { get; }

    public event AsyncEventHandler? Disposed;

    public Task<IResultCursor> RunAsync(
        Query query,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        _logger.LogDebug("Session auto-commit: {query}", query.Text);
        return _autoCommitRunner.RunAsync(query);
    }

    public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action)
    {
        return RunAsync(query, action, false);
    }

    public Task<IResultCursor> RunAsync(Query query)
    {
        return RunAsync(query, null!);
    }

    public Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action)
    {
        return RunAsync(new Query(query), action);
    }

    public Task<IResultCursor> RunAsync(
        string query,
        IDictionary<string, object> parameters,
        Action<TransactionConfigBuilder> action)
    {
        return RunAsync(new Query(query, parameters), action);
    }

    public Task<IResultCursor> RunAsync(string query)
    {
        return RunAsync(new Query(query));
    }

    public Task<IResultCursor> RunAsync(string query, object parameters)
    {
        return RunAsync(new Query(query, parameters));
    }

    public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters)
    {
        return RunAsync(new Query(query, parameters));
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        return BeginTransactionAsync(AccessMode.Write, action, disposeUnconsumedSessionResult);
    }

    public async Task<IAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        EnsureNoOpenTransaction();
        _logger.LogDebug("Session beginning {mode} transaction", mode);
        var tx = await _transactionFactory
            .BeginTransactionAsync(mode, action)
            .ConfigureAwait(false);
        _openTransaction = tx;
        return tx;
    }

    private void EnsureNoOpenTransaction()
    {
        if (_openTransaction?.IsOpen == true)
        {
            throw new TransactionNestingException(
                "Attempting to nest transactions. A session can only have a single " +
                "transaction open at a time. Commit or rollback the previous transaction before opening the next.");
        }
    }

    public Task<IAsyncTransaction> BeginTransactionAsync()
    {
        return BeginTransactionAsync(null!);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action)
    {
        return BeginTransactionAsync(action, false);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode)
    {
        return BeginTransactionAsync(mode, null!);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode, Action<TransactionConfigBuilder> action)
    {
        return BeginTransactionAsync(mode, action, false);
    }

    public Task<TResult> ExecuteReadAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> txFuncAsync,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunTransactionWithRetryAsync(AccessMode.Read, txFuncAsync, action);
    }

    public Task ExecuteReadAsync(
        Func<IAsyncQueryRunner, Task> txFuncAsync,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunTransactionWithRetryAsync(AccessMode.Read, Adapt(txFuncAsync), action);
    }

    public Task<TResult> ExecuteWriteAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> txFuncAsync,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunTransactionWithRetryAsync(AccessMode.Write, txFuncAsync, action);
    }

    public Task ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task> txFuncAsync,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunTransactionWithRetryAsync(AccessMode.Write, Adapt(txFuncAsync), action);
    }

    public Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunTransactionWithRetryAsync(AccessMode.Read, func, null);
    }

    public Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunTransactionWithRetryAsync(AccessMode.Write, func, null);
    }

    private static Func<IAsyncQueryRunner, Task<bool>> Adapt(Func<IAsyncQueryRunner, Task> txFuncAsync)
    {
        return Adapter;

        async Task<bool> Adapter(IAsyncQueryRunner runner)
        {
            await txFuncAsync(runner).ConfigureAwait(false);
            return true;
        }
    }

    public Task CloseAsync() => DisposeAsync().AsTask();

    public async ValueTask DisposeAsync()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        await Disposed.FireAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private Task<TResult> RunTransactionWithRetryAsync<TResult>(
        AccessMode mode,
        Func<IAsyncQueryRunner, Task<TResult>> txFuncAsync,
        Action<TransactionConfigBuilder>? action)
    {
        return _retryLogic.RetryAsync(() => RunTransactionAsync(mode, txFuncAsync, action));
    }

    private async Task<TResult> RunTransactionAsync<TResult>(
        AccessMode mode,
        Func<IAsyncQueryRunner, Task<TResult>> txFuncAsync,
        Action<TransactionConfigBuilder>? action)
    {
        var tx = await _transactionFactory
            .BeginTransactionAsync(mode, action)
            .ConfigureAwait(false);

        try
        {
            _logger.LogDebug("Session beginning work", mode);
            var result = await txFuncAsync(tx).ConfigureAwait(false);
            await tx.CommitAsync().ConfigureAwait(false);
            return result;
        }
        catch
        {
            try
            {
                await tx.RollbackIfOpenAsync().ConfigureAwait(false);
            }
            catch
            {
                /* best-effort; don't mask the original error */
            }

            throw;
        }
    }
}
