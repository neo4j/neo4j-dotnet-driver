// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Telemetry;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;

namespace Neo4j.Driver.Internal;

internal partial class AsyncSession : AsyncQueryRunner, IInternalAsyncSession
{
    private readonly IBookmarkManager _bookmarkManager;

    // If the connection is ever successfully created, 
    // then it is session's responsibility to dispose them properly
    // without any possible connection leak.
    private readonly IConnectionProvider _connectionProvider;

    private readonly AccessMode _defaultMode;
    private readonly long _fetchSize;

    private readonly ILogger _logger;
    private readonly INotificationsConfig _notificationsConfig;
    private readonly bool _reactive;

    private readonly IAsyncRetryLogic _retryLogic;
    private readonly bool _useBookmarkManager;

    private IConnection _connection;

    private string _database;
    private bool _disposed;
    private Bookmarks _initialBookmarks;
    private bool _isOpen = true;
    private Task<IResultCursor> _result; // last session run result if any

    private AsyncTransaction _transaction;

    public AsyncSession(
        IConnectionProvider provider,
        ILogger logger,
        IAsyncRetryLogic retryLogic,
        long defaultFetchSize,
        SessionConfig config,
        bool reactive
    )
    {
        SessionConfig = config;
        _connectionProvider = provider;
        _logger = logger;
        _retryLogic = retryLogic;
        _reactive = reactive;

        _database = config.Database;
        _defaultMode = config.DefaultAccessMode;
        _fetchSize = config.FetchSize ?? defaultFetchSize;
        _notificationsConfig = config.NotificationsConfig;

        _useBookmarkManager = config.BookmarkManager != null;
        if (_useBookmarkManager)
        {
            _bookmarkManager = config.BookmarkManager;
        }

        if (config.Bookmarks != null)
        {
            LastBookmarks = Bookmarks.From(config.Bookmarks);
            _initialBookmarks = LastBookmarks;
        }
    }

    [Obsolete("Replaced by more sensibly named LastBookmarks. Will be removed in 6.0")]
    public Bookmark LastBookmark => LastBookmarks;

    public Bookmarks LastBookmarks { get; private set; }

    public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action)
    {
        return RunAsync(query, action, true);
    }

    public SessionConfig SessionConfig { get; }

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

    public override Task<IResultCursor> RunAsync(Query query)
    {
        return RunAsync(query, null);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync()
    {
        return BeginTransactionAsync(null);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action)
    {
        return BeginTransactionAsync(action, true);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        return BeginTransactionAsync(_defaultMode, action, disposeUnconsumedSessionResult);
    }

    public async Task<IAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        using var _ = Driver.TelemetryManager.StartApiActivity(QueryApiType.UnmanagedTransaction);

        var tx = await TryExecuteAsync(
                _logger,
                () => BeginTransactionWithoutLoggingAsync(mode, action, disposeUnconsumedSessionResult))
            .ConfigureAwait(false);

        return tx;
    }

    public Task<IResultCursor> RunAsync(
        Query query,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        using var _ = Driver.TelemetryManager.StartApiActivity(QueryApiType.AutoCommit);
        var options = BuildTransactionConfig(action);
        var result = TryExecuteAsync(
            _logger,
            async () =>
            {
                await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

                await AcquireConnectionAndDbNameAsync(_defaultMode).ConfigureAwait(false);

                if (_useBookmarkManager)
                {
                    LastBookmarks = await GetBookmarksAsync().ConfigureAwait(false);
                }

                return await _connection
                    .RunInAutoCommitTransactionAsync(
                        new AutoCommitParams
                        {
                            Query = query,
                            Reactive = _reactive,
                            Database = _database,
                            Bookmarks = LastBookmarks,
                            Config = options,
                            SessionConfig = SessionConfig,
                            FetchSize = _fetchSize,
                            BookmarksTracker = this,
                            ResultResourceHandler = this
                        },
                        _notificationsConfig)
                    .ConfigureAwait(false);
            });

        _result = result;
        return result;
    }

    public Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<EagerResult<T>>> func)
    {
        return RunTransactionAsync(AccessMode.Read, func, null, false);
    }

    public Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<EagerResult<T>>> func)
    {
        return RunTransactionAsync(AccessMode.Write, func, null, false);
    }

    private TransactionConfig BuildTransactionConfig(Action<TransactionConfigBuilder> action)
    {
        if (action == null)
        {
            return TransactionConfig.Default;
        }
        var builder = new TransactionConfigBuilder(_logger, new TransactionConfig());
        action.Invoke(builder);
        return builder.Build();
    }

    public Task<T> ReadTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, action);
    }

    public Task ReadTransactionAsync(
        Func<IAsyncTransaction, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, action);
    }

    public Task<T> WriteTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, action);
    }

    public Task WriteTransactionAsync(
        Func<IAsyncTransaction, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, action);
    }

    public Task ExecuteReadAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, action);
    }

    public Task<T> ExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, action);
    }

    public Task ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, action);
    }

    public Task<T> ExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, action);
    }

    private async Task<Bookmarks> GetBookmarksAsync()
    {
        return _initialBookmarks == null
            ? Bookmarks.From(await _bookmarkManager.GetBookmarksAsync().ConfigureAwait(false))
            : Bookmarks.From(
                (await _bookmarkManager.GetBookmarksAsync().ConfigureAwait(false)).Concat(_initialBookmarks.Values));
    }

    private Task RunTransactionAsync(
        AccessMode mode,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> action)
    {
        return RunTransactionAsync(
            mode,
            async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            },
            action);
    }

    private Task RunTransactionAsync(
        AccessMode mode,
        Func<IAsyncTransaction, Task> work,
        Action<TransactionConfigBuilder> action,
        bool awaitBegin = true)
    {
        return RunTransactionAsync(
            mode,
            async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            },
            action,
            awaitBegin);
    }

    private Task<T> RunTransactionAsync<T>(
        AccessMode mode,
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action,
        bool awaitBegin = true)
    {
        using var _ = Driver.TelemetryManager.StartApiActivity(QueryApiType.TransactionFunction);
        return TryExecuteAsync(
            _logger,
            () => _retryLogic.RetryAsync(
                async () =>
                {
                    var tx = await BeginTransactionWithoutLoggingAsync(mode, action, true, awaitBegin).ConfigureAwait(false);
                    try
                    {
                        var result = await work(tx).ConfigureAwait(false);
                        if (tx.IsOpen)
                        {
                            await tx.CommitAsync().ConfigureAwait(false);
                        }

                        return result;
                    }
                    catch
                    {
                        if (tx.IsOpen)
                        {
                            await tx.RollbackAsync().ConfigureAwait(false);
                        }

                        throw;
                    }
                }));
    }

    private async Task<IInternalAsyncTransaction> BeginTransactionWithoutLoggingAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult,
        bool awaitBegin = true)
    {
        var config = BuildTransactionConfig(action);
        await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

        await AcquireConnectionAndDbNameAsync(mode).ConfigureAwait(false);
        if (_useBookmarkManager)
        {
            LastBookmarks = await GetBookmarksAsync().ConfigureAwait(false);
        }

        var tx = new AsyncTransaction(
            _connection,
            this,
            _logger,
            _database,
            LastBookmarks,
            _reactive,
            _fetchSize,
            SessionConfig,
            _notificationsConfig);

        await tx.BeginTransactionAsync(config, awaitBegin).ConfigureAwait(false);
        _transaction = tx;
        return _transaction;
    }

    private async Task AcquireConnectionAndDbNameAsync(AccessMode mode, bool forceAuth = false)
    {
        if (_useBookmarkManager)
        {
            LastBookmarks = await GetBookmarksAsync().ConfigureAwait(false);
        }

        _connection = await _connectionProvider.AcquireAsync(
                mode,
                _database,
                SessionConfig,
                LastBookmarks,
                forceAuth)
            .ConfigureAwait(false);

        //Update the database. If a routing request occurred it may have returned a differing DB alias name that needs to be used for the
        //rest of the sessions lifetime.
        _database = _connection.Database;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            //Dispose managed resources

            //call it synchronously
            CloseAsync().GetAwaiter().GetResult();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CloseAsync().ConfigureAwait(false);
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private string ImpersonatedUser()
    {
        return SessionConfig is not null ? SessionConfig.ImpersonatedUser : string.Empty;
    }

    public async Task<bool> VerifyConnectivityAsync()
    {
        var authCodeExceptions = new[]
        {
            "Neo.ClientError.Security.CredentialsExpired",
            "Neo.ClientError.Security.Forbidden",
            "Neo.ClientError.Security.TokenExpired",
            "Neo.ClientError.Security.Unauthorized"
        };

        try
        {
            await AcquireConnectionAndDbNameAsync(AccessMode.Read, true).ConfigureAwait(false);
        }
        catch (Neo4jException neoException) when (authCodeExceptions.Contains(neoException.Code))
        {
            return false;
        }

        return true;
    }
}
