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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Extensions;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.Internal.Util;
using static Neo4j.Driver.Internal.ConnectionPoolStatus;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;
using static Neo4j.Driver.Internal.Throw.ObjectDisposedException;
using static Neo4j.Driver.Internal.Util.ConnectionContext;

namespace Neo4j.Driver.Internal;

internal sealed class ConnectionPool : IConnectionPool
{
    private const int SpinningWaitInterval = 500;
    private readonly TimeSpan _connectionAcquisitionTimeout;
    private readonly IPooledConnectionFactory _connectionFactory;

    private readonly IConnectionValidator _connectionValidator;

    private readonly string _id;

    private readonly BlockingCollection<IPooledConnection> _idleConnections = new();
    private readonly ConcurrentHashSet<IPooledConnection> _inUseConnections = new();

    private readonly ILogger _logger;
    private readonly int _maxIdlePoolSize;

    private readonly int _maxPoolSize;
    private readonly INotificationsConfig _notificationsConfig;

    private readonly IConnectionPoolListener _poolMetricsListener;

    private readonly object _poolSizeSync = new();

    private readonly Uri _uri;

    private int _poolSize;

    private ConnectionPoolStatus _poolStatus = Active;

    public ConnectionPool(
        Uri uri,
        IPooledConnectionFactory connectionFactory,
        ConnectionPoolSettings connectionPoolSettings,
        ILogger logger,
        ConnectionSettings connectionSettings,
        IDictionary<string, string> routingContext,
        INotificationsConfig notificationsConfig = null)
    {
        _uri = uri;
        _id = $"pool-{_uri.Host}:{_uri.Port}";
        _logger = new PrefixLogger(logger, $"[{_id}]");
        _maxPoolSize = connectionPoolSettings.MaxConnectionPoolSize;
        _maxIdlePoolSize = connectionPoolSettings.MaxIdleConnectionPoolSize;
        _connectionAcquisitionTimeout = connectionPoolSettings.ConnectionAcquisitionTimeout;

        _connectionFactory = connectionFactory;
        ConnectionSettings = connectionSettings;

        var connIdleTimeout = connectionPoolSettings.ConnectionIdleTimeout;
        var maxConnectionLifetime = connectionPoolSettings.MaxConnectionLifetime;
        _connectionValidator = new ConnectionValidator(connIdleTimeout, maxConnectionLifetime);

        var metrics = connectionPoolSettings.Metrics;
        _poolMetricsListener = metrics?.PutPoolMetrics($"{_id}-{GetHashCode()}", this);

        RoutingContext = routingContext;
        _notificationsConfig = notificationsConfig;
    }

    // Used in test only
    internal ConnectionPool(
        IPooledConnectionFactory connectionFactory,
        BlockingCollection<IPooledConnection> idleConnections = null,
        ConcurrentHashSet<IPooledConnection> inUseConnections = null,
        ConnectionPoolSettings poolSettings = null,
        ConnectionSettings connectionSettings = null,
        IConnectionValidator validator = null,
        ILogger logger = null,
        INotificationsConfig notificationsConfig = null)
        : this(
            new Uri("bolt://localhost:7687"),
            connectionFactory,
            poolSettings ?? new ConnectionPoolSettings(Config.Default),
            logger,
            connectionSettings,
            null,
            notificationsConfig)
    {
        _idleConnections = idleConnections ?? new BlockingCollection<IPooledConnection>();
        _inUseConnections = inUseConnections ?? new ConcurrentHashSet<IPooledConnection>();
        if (validator != null)
        {
            _connectionValidator = validator;
        }
    }

    private bool IsClosed => AtomicRead(ref _poolStatus) == Closed;
    private bool IsInactive => AtomicRead(ref _poolStatus) == Inactive;
    private bool IsInactiveOrClosed => AtomicRead(ref _poolStatus) != Active;
    internal int PoolSize => Interlocked.CompareExchange(ref _poolSize, -1, -1);
    public int NumberOfInUseConnections => _inUseConnections.Count;
    public int NumberOfIdleConnections => _idleConnections.Count;

    public IDictionary<string, string> RoutingContext { get; set; }

    public ConnectionPoolStatus Status
    {
        get => AtomicRead(ref _poolStatus);
        internal set => Interlocked.Exchange(ref _poolStatus, value);
    }

    public async Task<IConnection> AcquireAsync(
        AccessMode mode,
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks,
        bool forceAuth = false)
    {
        _poolMetricsListener?.PoolAcquiring();

        try
        {
            do
            {
                var connection = await TryExecuteAsync(
                        _logger,
                        () => AcquireOrTimeoutAsync(database, sessionConfig, mode, _connectionAcquisitionTimeout),
                        "Failed to acquire a connection from connection pool asynchronously.")
                    .ConfigureAwait(false);

                try
                {
                    connection.SessionConfig = sessionConfig;
                    if (forceAuth)
                    {
                        connection.AuthorizationStatus = AuthorizationStatus.AuthorizationExpired;
                    }

                    if (connection.AuthorizationStatus != AuthorizationStatus.FreshlyAuthenticated &&
                        connection.AuthorizationStatus != AuthorizationStatus.SessionToken)
                    {
                        await connection.ValidateCredsAsync().ConfigureAwait(false);
                        if (forceAuth)
                        {
                            await connection.SyncAsync().ConfigureAwait(false);
                        }
                    }

                    _poolMetricsListener?.PoolAcquired();
                    return connection;
                }
                catch (ReauthException ex)
                {
                    _logger.Debug(ex.ToString());
                    if (ex.IsUserSwitching)
                    {
                        throw;
                    }

                    await DestroyConnectionAsync(connection).ConfigureAwait(false);
                }
            } while (true);
        }
        catch
        {
            _poolMetricsListener?.PoolFailedToAcquire();
            throw;
        }
    }

    public async Task ReleaseAsync(IPooledConnection connection)
    {
        await TryExecuteAsync(
                _logger,
                async () =>
                {
                    if (IsClosed)
                    {
                        // pool already disposed
                        return;
                    }

                    if (!_inUseConnections.TryRemove(connection))
                    {
                        // pool already disposed
                        return;
                    }

                    if (!await _connectionValidator.OnReleaseAsync(connection).ConfigureAwait(false) ||
                        IsIdlePoolFull() ||
                        IsInactiveOrClosed)
                    {
                        // cannot return to idle pool
                        await DestroyConnectionAsync(connection).ConfigureAwait(false);
                        return;
                    }

                    connection.Configure(null, null);
                    connection.SessionConfig = null;

                    // Add back to idle pool
                    _idleConnections.Add(connection);
                    // Just dequeue any one connection and close it will ensure that all connections in the pool will finally be closed
                    if (IsInactiveOrClosed && _idleConnections.TryTake(out connection))
                    {
                        await DestroyConnectionAsync(connection).ConfigureAwait(false);
                    }
                },
                $"Failed to release connection '{connection}' asynchronously back to pool.")
            .ConfigureAwait(false);
    }

    public async Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
    {
        var connection = await AcquireAsync(AccessMode.Read, null, null, CancellationToken.None)
            .ConfigureAwait(false);

        if (connection is not IPooledConnection pooledConnection)
        {
            throw new Exception("AcquireAsync returned wrong connection type");
        }

        try
        {
            await pooledConnection.ResetAsync().ConfigureAwait(false);
        }
        finally
        {
            await ReleaseAsync(pooledConnection).ConfigureAwait(false);
        }

        return connection.Server;
    }

    public ConnectionSettings ConnectionSettings { get; }

    public Task<bool> SupportsMultiDbAsync()
    {
        return CheckConnectionSupport(c => c.SupportsMultiDatabase());
    }

    public Task<bool> SupportsReAuthAsync()
    {
        return CheckConnectionSupport(c => c.SupportsReAuth());
    }

    public IRoutingTable GetRoutingTable(string database)
    {
        throw new NotSupportedException(
            "Should not be getting a routing table on a connection pool when it is the connection provider to the driver. Only Loadbalancer should do that.");
    }

    public Task DeactivateAsync()
    {
        if (Interlocked.CompareExchange(ref _poolStatus, Inactive, Active) == Active)
        {
            return Task.WhenAll(TerminateIdleConnectionsAsync());
        }

        return Task.CompletedTask;
    }

    public void Activate()
    {
        Interlocked.CompareExchange(ref _poolStatus, Active, Inactive);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(CloseAsync());
    }

    public void OnPoolMemberException(IPooledConnection connection, Exception exception)
    {
        if (exception is TokenExpiredException)
        {
            // if a token exception occured the pool member, all connections in the pool that are using that token
            // should be closed. This is because the token is now invalid and all connections using it will fail.
            foreach (var conn in _inUseConnections)
            {
                if (connection.AuthToken.Equals(conn.AuthToken))
                {
                    conn.AuthorizationStatus = AuthorizationStatus.TokenExpired;
                }
            }
        }

        if (exception is AuthorizationException)
        {
            foreach (var conn in _inUseConnections)
            {
                if (connection.AuthToken.Equals(conn.AuthToken))
                {
                    conn.AuthorizationStatus = AuthorizationStatus.AuthorizationExpired;
                }
            }
        }
    }

    private async Task<T> CheckConnectionSupport<T>(Func<IConnection, T> check)
    {
        // Establish a connection with the server and immediately close it.
        var connection = await AcquireAsync(Simple.Mode, Simple.Database, null, Simple.Bookmarks)
            .ConfigureAwait(false);

        var multiDb = check(connection);
        await connection.CloseAsync().ConfigureAwait(false);

        return multiDb;
    }

    private async Task<IPooledConnection> CreateNewPooledConnectionAsync(
        SessionConfig sessionConfig,
        CancellationToken cancellationToken = default)
    {
        var conn = default(IPooledConnection);

        try
        {
            conn = await NewPooledConnection(sessionConfig).ConfigureAwait(false);

            if (conn == null)
            {
                return null;
            }

            await conn
                .InitAsync(_notificationsConfig, sessionConfig, cancellationToken)
                .ConfigureAwait(false);

            _poolMetricsListener?.ConnectionCreated();
            return conn;
        }
        catch
        {
            _poolMetricsListener?.ConnectionFailedToCreate();

            // shut down and clean all the resources of the connection if failed to establish
            await DestroyConnectionAsync(conn).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IPooledConnection> NewPooledConnection(SessionConfig sessionConfig)
    {
        if (!TryIncrementPoolSize())
        {
            return null;
        }

        _poolMetricsListener?.ConnectionCreating();

        var token = sessionConfig?.AuthToken ??
            await ConnectionSettings.AuthTokenManager.GetTokenAsync().ConfigureAwait(false);

        return _connectionFactory.Create(
            _uri,
            this,
            ConnectionSettings.SocketSettings,
            token,
            ConnectionSettings.AuthTokenManager,
            ConnectionSettings.UserAgent,
            RoutingContext);
    }

    private async Task DestroyConnectionAsync(IPooledConnection conn)
    {
        DecrementPoolSize();

        if (conn == null)
        {
            return;
        }

        _poolMetricsListener?.ConnectionClosing();
        try
        {
            await conn.DestroyAsync().ConfigureAwait(false);
        }
        finally
        {
            _poolMetricsListener?.ConnectionClosed();
        }
    }

    /// <summary>
    /// Returns true if pool size is successfully increased, otherwise false. The reason to failed to increase the
    /// pool size probably due to the pool is full already
    /// </summary>
    /// <returns>true if pool size is successfully increased, otherwise false.</returns>
    private bool TryIncrementPoolSize()
    {
        if (_maxPoolSize == Config.Infinite)
        {
            Interlocked.Increment(ref _poolSize);
            return true;
        }

        if (PoolSize >= _maxPoolSize)
        {
            return false;
        }

        lock (_poolSizeSync)
        {
            if (PoolSize >= _maxPoolSize)
            {
                return false;
            }

            Interlocked.Increment(ref _poolSize);
            return true;
        }
    }

    private void DecrementPoolSize()
    {
        Interlocked.Decrement(ref _poolSize);
    }

    private async Task<IPooledConnection> AcquireOrTimeoutAsync(
        string database,
        SessionConfig sessionConfig,
        AccessMode mode,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await AcquireAsync(mode, database, sessionConfig, cts.Token)
                .Timeout(timeout, cts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            _poolMetricsListener?.PoolTimedOutToAcquire();
            if (cts.Token.IsCancellationRequested)
            {
                throw new ClientException(
                    $"Failed to obtain a connection from pool within {_connectionAcquisitionTimeout}");
            }

            throw new ClientException("Failed to obtain a connection from pool");
        }
    }

    private async Task<IPooledConnection> AcquireAsync(
        AccessMode mode,
        string database,
        SessionConfig sessionConfig,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            if (IsClosed)
            {
                throw GetDriverDisposedException(nameof(ConnectionPool));
            }

            if (IsInactive)
            {
                ThrowServerUnavailableExceptionDueToDeactivated();
            }

            var connection =
                await GetPooledOrNewConnectionAsync(sessionConfig, cancellationToken).ConfigureAwait(false);

            if (_connectionValidator.OnRequire(connection))
            {
                await AddConnectionAsync(connection).ConfigureAwait(false);

                connection.Configure(database, mode);

                return connection;
            }

            await DestroyConnectionAsync(connection).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async ValueTask AddConnectionAsync(IPooledConnection connection)
    {
        _inUseConnections.TryAdd(connection);

        if (!IsClosed)
        {
            return;
        }

        if (_inUseConnections.TryRemove(connection))
        {
            await DestroyConnectionAsync(connection).ConfigureAwait(false);
        }

        throw GetDriverDisposedException(nameof(ConnectionPool));
    }

    private Task<IPooledConnection> GetPooledOrNewConnectionAsync(
        SessionConfig sessionConfig,
        CancellationToken cancellationToken)
    {
        if (_idleConnections.TryTake(out var connection))
        {
            if (connection.AuthorizationStatus == AuthorizationStatus.FreshlyAuthenticated)
            {
                connection.AuthorizationStatus = AuthorizationStatus.Pooled;
            }

            return Task.FromResult(connection);
        }

        return CreateNewConnectionOrGetIdleAsync(sessionConfig, cancellationToken);
    }

    private async Task<IPooledConnection> CreateNewConnectionOrGetIdleAsync(
        SessionConfig sessionConfig,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsConnectionPoolFull())
            {
                var connection = await CreateNewPooledConnectionAsync(sessionConfig, cancellationToken)
                    .ConfigureAwait(false);

                if (connection != null)
                {
                    return connection;
                }
            }

            await Task.Delay(SpinningWaitInterval, cancellationToken).ConfigureAwait(false);
            if (_idleConnections.TryTake(out var idle))
            {
                if (idle.AuthorizationStatus == AuthorizationStatus.FreshlyAuthenticated)
                {
                    idle.AuthorizationStatus = AuthorizationStatus.Pooled;
                }

                return idle;
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private bool IsConnectionPoolFull()
    {
        return _maxPoolSize != Config.Infinite && PoolSize >= _maxPoolSize;
    }

    private bool IsIdlePoolFull()
    {
        return _maxIdlePoolSize != Config.Infinite && _idleConnections.Count >= _maxIdlePoolSize;
    }

    public Task CloseAsync()
    {
        if (Interlocked.Exchange(ref _poolStatus, Closed) != Closed)
        {
            return CloseAllConnectionsAsync();
        }

        return Task.CompletedTask;
    }

    private IEnumerable<Task> TerminateIdleConnectionsAsync()
    {
        var allCloseTasks = new List<Task>();
        while (_idleConnections.TryTake(out var connection))
        {
            _logger?.Debug($"Disposing Available Connection {connection}");
            allCloseTasks.Add(DestroyConnectionAsync(connection));
        }

        return allCloseTasks;
    }

    private void ThrowServerUnavailableExceptionDueToDeactivated()
    {
        throw new ServiceUnavailableException(
            $"Failed to acquire a connection from connection pool for server with URI `{_uri}` " +
            "as this server has already been removed from routing table. ");
    }

    private static ConnectionPoolStatus AtomicRead(ref ConnectionPoolStatus value)
    {
        // change to the same value,
        // a.k.a. do nothing but return the original value
        return Interlocked.CompareExchange(ref value, value, value);
    }

    public override string ToString()
    {
        return $"{nameof(_id)}: {{{_id}}}, {nameof(_idleConnections)}: {{{_idleConnections.ToContentString()}}}, " +
            $"{nameof(_inUseConnections)}: {{{_inUseConnections}}}";
    }

    private Task CloseAllConnectionsAsync()
    {
        var allCloseTasks = new List<Task>();

        foreach (var inUseConnection in _inUseConnections)
        {
            _logger?.Info($"Disposing In Use Connection {inUseConnection}");

            if (_inUseConnections.TryRemove(inUseConnection))
            {
                allCloseTasks.Add(DestroyConnectionAsync(inUseConnection));
            }
        }

        allCloseTasks.AddRange(TerminateIdleConnectionsAsync());

        return Task.WhenAll(allCloseTasks);
    }
}
