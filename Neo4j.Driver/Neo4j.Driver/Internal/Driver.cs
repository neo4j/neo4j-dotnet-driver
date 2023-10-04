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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal sealed class Driver : IInternalDriver
{
    private readonly DefaultBookmarkManager _bookmarkManager;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IAsyncRetryLogic _retryLogic;
    private int _closedMarker;

    internal Driver(
        Uri uri,
        IConnectionProvider connectionProvider,
        IAsyncRetryLogic retryLogic,
        DriverContext driverContext)
    {
        Uri = uri;
        Context = driverContext;
        _retryLogic = retryLogic;
        Config = driverContext.Config;
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _bookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig());
    }

    private bool IsClosed => _closedMarker > 0;

    internal DriverContext Context { get; }
    public Uri Uri { get; }
    public bool Encrypted => Context.EncryptionManager.UseTls;
    public Config Config { get; }

    public IAsyncSession AsyncSession()
    {
        return AsyncSession(null);
    }

    public IAsyncSession AsyncSession(Action<SessionConfigBuilder> action)
    {
        return Session(action, false);
    }

    public IInternalAsyncSession Session(Action<SessionConfigBuilder> action, bool reactive)
    {
        if (IsClosed)
        {
            ThrowDriverClosedException();
        }

        var sessionConfig = ConfigBuilders.BuildSessionConfig(action);

        var session = new AsyncSession(
            _connectionProvider,
            Config.Logger,
            _retryLogic,
            Config.FetchSize,
            sessionConfig,
            reactive);

        if (IsClosed)
        {
            ThrowDriverClosedException();
        }

        return session;
    }

    public Task<EagerResult<TResult>> ExecuteQueryAsync<TResult>(
        Query query,
        Func<IAsyncEnumerable<IRecord>, Task<TResult>> streamProcessor,
        QueryConfig config = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteQueryAsyncInternal(query, config, cancellationToken, TransformCursor(streamProcessor));
    }

    public Task CloseAsync()
    {
        return Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0
            ? _connectionProvider.DisposeAsync().AsTask()
            : Task.CompletedTask;
    }

    public Task<IServerInfo> GetServerInfoAsync()
    {
        return _connectionProvider.VerifyConnectivityAndGetInfoAsync();
    }

    public async Task<bool> TryVerifyConnectivityAsync()
    {
        try
        {
            await _connectionProvider.VerifyConnectivityAndGetInfoAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task VerifyConnectivityAsync()
    {
        return GetServerInfoAsync();
    }

    public Task<bool> SupportsMultiDbAsync()
    {
        return _connectionProvider.SupportsMultiDbAsync();
    }

    public Task<bool> SupportsSessionAuthAsync()
    {
        return _connectionProvider.SupportsReAuthAsync();
    }

    public void Dispose()
    {
        Dispose(true);
    }

    public ValueTask DisposeAsync()
    {
        return Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0
            ? _connectionProvider.DisposeAsync()
            : new ValueTask(Task.CompletedTask);
    }

    public async Task<ExecutionSummary> GetRowsAsync(
        Query query,
        QueryConfig config,
        Action<IRecord> streamProcessor,
        CancellationToken cancellationToken
    )
    {
        async Task<int> Process(IAsyncEnumerable<IRecord> records)
        {
            await foreach (var record in records.ConfigureAwait(false))
            {
                streamProcessor(record);
            }

            return 0;
        }

        var eagerResult = await ExecuteQueryAsyncInternal(
                query,
                config,
                cancellationToken,
                TransformCursor(Process))
            .ConfigureAwait(false);

        return new ExecutionSummary(eagerResult.Summary, eagerResult.Keys);
    }

    public IExecutableQuery<IRecord, IRecord> ExecutableQuery(string cypher)
    {
        return new ExecutableQuery<IRecord, IRecord>(new DriverRowSource(this, cypher), x => x);
    }

    public async Task<bool> VerifyAuthenticationAsync(IAuthToken authToken)
    {
        var session = AsyncSession(x => x.WithAuthToken(authToken).WithDatabase("system")) as AsyncSession;
        await using (session.ConfigureAwait(false))
        {
            return await session.VerifyConnectivityAsync().ConfigureAwait(false);
        }
    }

    //Non public facing api. Used for testing with testkit only
    public IRoutingTable GetRoutingTable(string database)
    {
        return _connectionProvider.GetRoutingTable(database);
    }

    private void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    private void Dispose(bool disposing)
    {
        if (IsClosed)
        {
            return;
        }

        if (disposing)
        {
            Close();
        }
    }

    private static void ThrowDriverClosedException()
    {
        throw new ObjectDisposedException(
            nameof(Driver),
            "Cannot open a new session on a driver that is already disposed.");
    }

    private async Task<EagerResult<T>> ExecuteQueryAsyncInternal<T>(
        Query query,
        QueryConfig config,
        CancellationToken cancellationToken,
        Func<IResultCursor, CancellationToken, Task<EagerResult<T>>> cursorProcessor)
    {
        query = query ?? throw new ArgumentNullException(nameof(query));
        config ??= new QueryConfig();

        var session = Session(x => ApplyConfig(config, x), false);
        await using (session.ConfigureAwait(false))
        {
            if (config.Routing == RoutingControl.Readers)
            {
                return await session.PipelinedExecuteReadAsync(x => Work(query, x, cursorProcessor, cancellationToken))
                    .ConfigureAwait(false);
            }

            return await session.PipelinedExecuteWriteAsync(x => Work(query, x, cursorProcessor, cancellationToken))
                .ConfigureAwait(false);
        }
    }

    private static Func<IResultCursor, CancellationToken, Task<EagerResult<TResult>>> TransformCursor<TResult>(
        Func<IAsyncEnumerable<IRecord>, Task<TResult>> streamProcessor)
    {
        async Task<EagerResult<TResult>> TransformCursorImpl(
            IResultCursor cursor,
            CancellationToken cancellationToken)
        {
            var processedStream = await streamProcessor(cursor).ConfigureAwait(false);
            var summary = await cursor.ConsumeAsync().ConfigureAwait(false);
            var keys = await cursor.KeysAsync().ConfigureAwait(false);
            return new EagerResult<TResult>(processedStream, summary, keys);
        }

        return TransformCursorImpl;
    }

    private void ApplyConfig(QueryConfig config, SessionConfigBuilder sessionConfigBuilder)
    {
        if (!string.IsNullOrWhiteSpace(config.Database))
        {
            sessionConfigBuilder.WithDatabase(config.Database);
        }

        if (!string.IsNullOrWhiteSpace(config.ImpersonatedUser))
        {
            sessionConfigBuilder.WithImpersonatedUser(config.ImpersonatedUser);
        }

        if (config.EnableBookmarkManager)
        {
            sessionConfigBuilder.WithBookmarkManager(config.BookmarkManager ?? _bookmarkManager);
        }

        sessionConfigBuilder.WithDefaultAccessMode(
            config.Routing switch
            {
                RoutingControl.Readers => AccessMode.Read,
                RoutingControl.Writers => AccessMode.Write,
                _ => throw new ArgumentOutOfRangeException()
            });
    }

    private static async Task<T> Work<T>(
        Query q,
        IAsyncQueryRunner x,
        Func<IResultCursor, CancellationToken, Task<T>> process,
        CancellationToken cancellationToken)
    {
        var cursor = await x.RunAsync(q).ConfigureAwait(false);
        return await process(cursor, cancellationToken).ConfigureAwait(false);
    }
}
