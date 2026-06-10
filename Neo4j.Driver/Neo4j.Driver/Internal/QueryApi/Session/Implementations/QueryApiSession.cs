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
    private readonly IQueryApiTransactionFactory _transactionFactory;

    private bool _closed;

    public QueryApiSession(
        SessionConfig sessionConfig,
        IAutoCommitRunner autoCommitRunner,
        IQueryApiTransactionFactory transactionFactory,
        IBookmarkTracker bookmarkTracker,
        ILogger logger)
    {
        SessionConfig = sessionConfig;
        _autoCommitRunner = autoCommitRunner;
        _transactionFactory = transactionFactory;
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
        _logger.Debug("Session auto-commit: {query}", query.Text);
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
        _logger.Debug("Session beginning {mode} transaction", mode);
        return await _transactionFactory
            .BeginTransactionAsync(mode, action)
            .ConfigureAwait(false);
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
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunManagedTransactionAsync(AccessMode.Read, work, action);
    }

    public Task ExecuteReadAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunManagedTransactionAsync(AccessMode.Read, work, action);
    }

    public Task<TResult> ExecuteWriteAsync<TResult>(
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunManagedTransactionAsync(AccessMode.Write, work, action);
    }

    public Task ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder>? action = null)
    {
        return RunManagedTransactionAsync(AccessMode.Write, work, action);
    }

    public Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunManagedTransactionAsync(AccessMode.Read, func, null);
    }

    public Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunManagedTransactionAsync(AccessMode.Write, func, null);
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

    private async Task<TResult> RunManagedTransactionAsync<TResult>(
        AccessMode mode,
        Func<IAsyncQueryRunner, Task<TResult>> work,
        Action<TransactionConfigBuilder>? action)
    {
        var tx = await _transactionFactory
            .BeginTransactionAsync(mode, action)
            .ConfigureAwait(false);

        try
        {
            _logger.Debug("Session beginning work", mode);
            var result = await work(tx).ConfigureAwait(false);
            await tx.CommitAsync().ConfigureAwait(false);
            return result;
        }
        catch
        {
            if (!tx.IsOpen)
            {
                throw;
            }

            try
            {
                await tx.RollbackAsync().ConfigureAwait(false);
            }
            catch
            {
                /* best-effort; don't mask the original error */
            }

            throw;
        }
    }

    private async Task RunManagedTransactionAsync(
        AccessMode mode,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder>? action)
    {
        var tx = await _transactionFactory
            .BeginTransactionAsync(mode, action)
            .ConfigureAwait(false);

        try
        {
            await work(tx).ConfigureAwait(false);
            await tx.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            if (tx.IsOpen)
            {
                try
                {
                    await tx.RollbackAsync().ConfigureAwait(false);
                }
                catch
                {
                    /* best-effort; don't mask the original error */
                }
            }

            throw;
        }
    }
}
