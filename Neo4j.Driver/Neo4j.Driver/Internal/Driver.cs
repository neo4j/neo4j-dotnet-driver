// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Experimental;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal sealed class Driver : IInternalDriver
{
    private int _closedMarker = 0;

    private readonly IConnectionProvider _connectionProvider;
    private readonly IAsyncRetryLogic _retryLogic;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly Config _config;
    private readonly DefaultBookmarkManager _bookmarkManager;

    public Uri Uri { get; }
    public bool Encrypted { get; }

    internal Driver(
        Uri uri,
        bool encrypted,
        IConnectionProvider connectionProvider,
        IAsyncRetryLogic retryLogic,
        ILogger logger = null,
        IMetrics metrics = null,
        Config config = null)
    {
        Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));

        Uri = uri;
        Encrypted = encrypted;
        _logger = logger;
        _connectionProvider = connectionProvider;
        _retryLogic = retryLogic;
        _metrics = metrics;
        _config = config;
        _bookmarkManager = new DefaultBookmarkManager(new BookmarkManagerConfig(null, null, null));
    }

    private bool IsClosed => _closedMarker > 0;

    public Config Config => _config;

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
            _logger,
            _retryLogic,
            _config.FetchSize,
            sessionConfig,
            reactive);

        if (IsClosed)
        {
            ThrowDriverClosedException();
        }

        return session;
    }

    private void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    public Task CloseAsync()
    {
        return Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0
            ? _connectionProvider.CloseAsync()
            : Task.CompletedTask;
    }

    public Task<IServerInfo> GetServerInfoAsync() =>
        _connectionProvider.VerifyConnectivityAndGetInfoAsync();

    public Task VerifyConnectivityAsync() => GetServerInfoAsync();

    public Task<bool> SupportsMultiDbAsync()
    {
        return _connectionProvider.SupportsMultiDbAsync();
    }

    //Non public facing api. Used for testing with testkit only
    public IRoutingTable GetRoutingTable(string database)
    {
        return _connectionProvider.GetRoutingTable(database);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (IsClosed)
            return;

        if (disposing)
        {
            Close();
        }
    }

    public ValueTask DisposeAsync()
    {
        return IsClosed ? default : new ValueTask(CloseAsync());
    }

    private static void ThrowDriverClosedException()
    {
        throw new ObjectDisposedException(
            nameof(Driver),
            "Cannot open a new session on a driver that is already disposed.");
    }

    internal IMetrics GetMetrics()
    {
        if (_metrics == null)
        {
            throw new InvalidOperationException(
                "Cannot access driver metrics if it is not enabled when creating this driver.");
        }

        return _metrics;
    }

    private async Task<EagerResult<T>> ExecuteQueryAsyncInternal<T>(
        Query query,
        QueryConfig config,
        CancellationToken cancellationToken,
        Func<IResultCursor, CancellationToken, Task<EagerResult<T>>> cursorProcessor)
    {
        query = query ?? throw new ArgumentNullException(nameof(query));
        config ??= new QueryConfig();

        var session = AsyncSession(x => ApplyConfig(config, x));
        await using (session.ConfigureAwait(false))
        {
            if (config.Routing == RoutingControl.Readers)
                return await session.ExecuteReadAsync(x => Work(query, x, cursorProcessor, cancellationToken))
                    .ConfigureAwait(false);

            return await session.ExecuteWriteAsync(x => Work(query, x, cursorProcessor, cancellationToken))
                .ConfigureAwait(false);
        }
    }

    public Task<EagerResult<TResult>> ExecuteQueryAsync<TResult>(
        Query query,
        Func<IAsyncEnumerable<IRecord>, ValueTask<TResult>> streamProcessor,
        QueryConfig config = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteQueryAsyncInternal(
            query,
            config,
            cancellationToken,
            TransformCursor(streamProcessor));
    }

    private static Func<IResultCursor, CancellationToken, Task<EagerResult<TResult>>> TransformCursor<TResult>(
        Func<IAsyncEnumerable<IRecord>, ValueTask<TResult>> streamProcessor)
    {
        async Task<EagerResult<TResult>> TransformCursorImpl(
            IResultCursor cursor,
            CancellationToken cancellationToken)
        {
            var processedStream = await streamProcessor(cursor);
            var summary = await cursor.ConsumeAsync().ConfigureAwait(false);
            var keys = await cursor.KeysAsync();
            return new EagerResult<TResult>(processedStream, summary, keys);
        }

        return TransformCursorImpl;
    }

    private void ApplyConfig(QueryConfig config, SessionConfigBuilder sessionConfigBuilder)
    {
        if (!string.IsNullOrWhiteSpace(config.Database))
            sessionConfigBuilder.WithDatabase(config.Database);

        if (!string.IsNullOrWhiteSpace(config.ImpersonatedUser))
            sessionConfigBuilder.WithImpersonatedUser(config.ImpersonatedUser);

        if (config.EnableBookmarkManager)
            sessionConfigBuilder.WithBookmarkManager(config.BookmarkManager ?? _bookmarkManager);

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
