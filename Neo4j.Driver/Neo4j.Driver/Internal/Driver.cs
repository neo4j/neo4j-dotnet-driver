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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal;

internal sealed class Driver : IInternalDriver
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly IAsyncRetryLogic _retryLogic;
    private int _closedMarker;

    internal Driver(
        Uri uri,
        bool encrypted,
        IConnectionProvider connectionProvider,
        IAsyncRetryLogic retryLogic,
        ILogger logger = null,
        IMetrics metrics = null,
        Config config = null)
    {
        Uri = uri;
        Encrypted = encrypted;
        _logger = logger;
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _retryLogic = retryLogic;
        _metrics = metrics;
        Config = config;
    }

    public Uri Uri { get; }

    private bool IsClosed => _closedMarker > 0;
    public bool Encrypted { get; }

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
            _logger,
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

    public Task CloseAsync()
    {
        return DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
        {
            await _connectionProvider.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task<IServerInfo> GetServerInfoAsync()
    {
        return _connectionProvider.VerifyConnectivityAndGetInfoAsync();
    }

    public Task VerifyConnectivityAsync()
    {
        return GetServerInfoAsync();
    }

    public Task<bool> SupportsMultiDbAsync()
    {
        return _connectionProvider.SupportsMultiDbAsync();
    }

    public void Dispose()
    {
        if (IsClosed)
        {
            return;
        }

        DisposeAsync().GetAwaiter().GetResult();
    }

    //Non public facing api. Used for testing with testkit only
    public IRoutingTable GetRoutingTable(string database)
    {
        return _connectionProvider.GetRoutingTable(database);
    }

    private void ThrowDriverClosedException()
    {
        throw new ObjectDisposedException(
            GetType().Name,
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
}
