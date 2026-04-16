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
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
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
    private readonly DriverContext _driverContext;
    private readonly long _fetchSize;

    private readonly INeo4jLogger _neo4JLogger;
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
        INeo4jLogger neo4JLogger,
        IAsyncRetryLogic retryLogic,
        long defaultFetchSize,
        SessionConfig config,
        bool reactive,
        bool telemetryEnabled)
    {
        SessionConfig = config;
        _connectionProvider = provider;
        _neo4JLogger = neo4JLogger;
        _retryLogic = retryLogic;
        _reactive = reactive;
        _driverContext = config.DriverContext;;

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

        TelemetryEnabled = telemetryEnabled;

        config.OnPinDatabase = OnPinDatabase;
    }

    private void OnPinDatabase(string db)
    {
        if(_connectionProvider.IsDirectDriver)
        {
            // don't pin
            return;
        }

        if (string.IsNullOrWhiteSpace(_database))
        {
            _neo4JLogger.Info($"Database '{db}' is pinned to the session.");
            _database = db;
        }
        else
        {
            _neo4JLogger.Info($"Database {_database} is already pinned to the session, ignoring {db}.");
        }
    }

    internal bool TelemetryEnabled { get; set; }

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

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode)
    {
        return BeginTransactionAsync(mode, null);
    }

    public Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode, Action<TransactionConfigBuilder> action)
    {
        return BeginTransactionAsync(mode, action, true);
    }

    public async Task<IAsyncTransaction> BeginTransactionAsync(
        AccessMode mode,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        var config = BuildTransactionConfig(action);
        return await TryExecuteAsync(
                _neo4JLogger,
                () => BeginTransactionWithoutLoggingAsync(
                    mode,
                    config,
                    disposeUnconsumedSessionResult,
                    new TransactionInfo(QueryApiType.UnmanagedTransaction, TelemetryEnabled, true)))
            .ConfigureAwait(false);
    }

    public Task<IResultCursor> RunAsync(
        Query query,
        Action<TransactionConfigBuilder> action,
        bool disposeUnconsumedSessionResult)
    {
        var options = BuildTransactionConfig(action);
        var result = TryExecuteAsync(
            _neo4JLogger,
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
                            Database = SessionConfig.Database ?? _database,
                            Bookmarks = LastBookmarks,
                            Config = options,
                            SessionConfig = SessionConfig,
                            FetchSize = _fetchSize,
                            BookmarksTracker = this,
                            ResultResourceHandler = this,
                            TransactionInfo = new TransactionInfo(
                                QueryApiType.AutoCommit,
                                TelemetryEnabled,
                                false)
                        },
                        _notificationsConfig,
                        _driverContext?.HomeDbCache)
                    .ConfigureAwait(false);
            });

        _result = result;
        return result;
    }

    public Task<EagerResult<T>> PipelinedExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunTransactionAsync(
            AccessMode.Read,
            func,
            config,
            new TransactionInfo(QueryApiType.DriverLevel, TelemetryEnabled, false));
    }

    public Task<EagerResult<T>> PipelinedExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<EagerResult<T>>> func,
        TransactionConfig config)
    {
        return RunTransactionAsync(
            AccessMode.Write,
            func,
            config,
            new TransactionInfo(QueryApiType.DriverLevel, TelemetryEnabled, false));
    }

    public Task<T> ReadTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, BuildTransactionConfig(action));
    }

    public Task ReadTransactionAsync(
        Func<IAsyncTransaction, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, BuildTransactionConfig(action));
    }

    public Task<T> WriteTransactionAsync<T>(
        Func<IAsyncTransaction, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, BuildTransactionConfig(action));
    }

    public Task WriteTransactionAsync(
        Func<IAsyncTransaction, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, BuildTransactionConfig(action));
    }

    public Task ExecuteReadAsync(Func<IAsyncQueryRunner, Task> work, Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, BuildTransactionConfig(action));
    }

    public Task<T> ExecuteReadAsync<T>(
        Func<IAsyncQueryRunner, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Read, work, BuildTransactionConfig(action));
    }

#pragma warning disable CS0618
    Task<IResultCursor> IAsyncSession.ExecuteReadAsync(
        Func<IAsyncQueryRunner, Task<IResultCursor>> work,
        Action<TransactionConfigBuilder> action)
    {
        throw new InvalidOperationException(
            "Do not return IResultCursor from a transaction function. The cursor is backed by the transaction, " +
            "which is committed and closed before the caller can use it. " +
            "Consume results inside the delegate instead, e.g. return await cursor.ToListAsync().");
    }
#pragma warning restore CS0618

    public Task ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, BuildTransactionConfig(action));
    }

    public Task<T> ExecuteWriteAsync<T>(
        Func<IAsyncQueryRunner, Task<T>> work,
        Action<TransactionConfigBuilder> action = null)
    {
        return RunTransactionAsync(AccessMode.Write, work, BuildTransactionConfig(action));
    }

#pragma warning disable CS0618
    Task<IResultCursor> IAsyncSession.ExecuteWriteAsync(
        Func<IAsyncQueryRunner, Task<IResultCursor>> work,
        Action<TransactionConfigBuilder> action)
    {
        throw new InvalidOperationException(
            "Do not return IResultCursor from a transaction function. The cursor is backed by the transaction, " +
            "which is committed and closed before the caller can use it. " +
            "Consume results inside the delegate instead, e.g. return await cursor.ToListAsync().");
    }
#pragma warning restore CS0618

    private TransactionConfig BuildTransactionConfig(Action<TransactionConfigBuilder> action)
    {
        if (action == null)
        {
            return TransactionConfig.Default;
        }

        var builder = new TransactionConfigBuilder(_neo4JLogger, new TransactionConfig());
        action.Invoke(builder);
        return builder.Build();
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
        Func<IAsyncTransaction, Task> work,
        TransactionConfig config,
        TransactionInfo transactionInfo = null)
    {
        return RunTransactionAsync(
            mode,
            async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            },
            config,
            transactionInfo);
    }

    private Task<T> RunTransactionAsync<T>(
        AccessMode mode,
        Func<IAsyncTransaction, Task<T>> work,
        TransactionConfig config,
        TransactionInfo transactionInfo = null)
    {
        transactionInfo ??= new TransactionInfo(QueryApiType.TransactionFunction, TelemetryEnabled, true);
        return TryExecuteAsync(
            _neo4JLogger,
            () => _retryLogic.RetryAsync(
                async () =>
                {
                    var tx = await BeginTransactionWithoutLoggingAsync(mode, config, true, transactionInfo)
                        .ConfigureAwait(false);

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
        TransactionConfig config,
        bool disposeUnconsumedSessionResult,
        TransactionInfo transactionInfo)
    {
        await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

        await AcquireConnectionAndDbNameAsync(mode).ConfigureAwait(false);
        if (_useBookmarkManager)
        {
            LastBookmarks = await GetBookmarksAsync().ConfigureAwait(false);
        }

        var tx = new AsyncTransaction(
            _connection,
            this,
            _neo4JLogger,
            _database,
            LastBookmarks,
            _reactive,
            _fetchSize,
            SessionConfig,
            _notificationsConfig,
            _driverContext);

        await tx.BeginTransactionAsync(config, transactionInfo).ConfigureAwait(false);
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
