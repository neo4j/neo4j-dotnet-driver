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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPool : IConnectionPool
    {
        private const int SpinningWaitInterval = 500;

        private readonly Uri _uri;

        private ConnectionPoolStatus _poolStatus = Active;
        private bool IsClosed => AtomicRead(ref _poolStatus) == Closed;
        private bool IsInactive => AtomicRead(ref _poolStatus) == Inactive;
        private bool IsInactiveOrClosed => AtomicRead(ref _poolStatus) != Active;

        private int _poolSize = 0;
        public int NumberOfInUseConnections => _inUseConnections.Count;
        public int NumberOfIdleConnections => _idleConnections.Count;
        internal int PoolSize => Interlocked.CompareExchange(ref _poolSize, -1, -1);

        private readonly int _maxPoolSize;
        private readonly int _maxIdlePoolSize;

        private readonly object _poolSizeSync = new object();
        private readonly TimeSpan _connectionAcquisitionTimeout;

        private readonly IConnectionValidator _connectionValidator;
        private readonly IPooledConnectionFactory _connectionFactory;

        private readonly BlockingCollection<IPooledConnection> _idleConnections = new BlockingCollection<IPooledConnection>();
        private readonly ConcurrentHashSet<IPooledConnection> _inUseConnections = new ConcurrentHashSet<IPooledConnection>();

        private readonly IConnectionPoolListener _poolMetricsListener;

        private readonly string _id;

        private readonly ILogger _logger;

        public IDictionary<string, string> RoutingContext { get; set; }

        public ConnectionPoolStatus Status
        {
            get => AtomicRead(ref _poolStatus);
            internal set => Interlocked.Exchange(ref _poolStatus, value);
        }

        public ConnectionPool(
            Uri uri,
            IPooledConnectionFactory connectionFactory,
            ConnectionPoolSettings connectionPoolSettings,
            ILogger logger,
            IDictionary<string, string> routingContext)
        {
            _uri = uri;
            _id = $"pool-{_uri.Host}:{_uri.Port}";
            _logger = new PrefixLogger(logger, $"[{_id}]");
            _maxPoolSize = connectionPoolSettings.MaxConnectionPoolSize;
            _maxIdlePoolSize = connectionPoolSettings.MaxIdleConnectionPoolSize;
            _connectionAcquisitionTimeout = connectionPoolSettings.ConnectionAcquisitionTimeout;

            _connectionFactory = connectionFactory;

            var connIdleTimeout = connectionPoolSettings.ConnectionIdleTimeout;
            var maxConnectionLifetime = connectionPoolSettings.MaxConnectionLifetime;
            _connectionValidator = new ConnectionValidator(connIdleTimeout, maxConnectionLifetime);

            var metrics = connectionPoolSettings.Metrics;
            _poolMetricsListener = metrics?.PutPoolMetrics($"{_id}-{GetHashCode()}", this);

            RoutingContext = routingContext;
        }

        // Used in test only
        internal ConnectionPool(
            IPooledConnectionFactory connectionFactory,
            BlockingCollection<IPooledConnection> idleConnections = null,
            ConcurrentHashSet<IPooledConnection> inUseConnections = null,
            ConnectionPoolSettings poolSettings = null,
            IConnectionValidator validator = null,
            ILogger logger = null)
            : this(new Uri("bolt://localhost:7687"), connectionFactory,
                poolSettings ?? new ConnectionPoolSettings(Config.Default), logger, null)
        {
            _idleConnections = idleConnections ?? new BlockingCollection<IPooledConnection>();
            _inUseConnections = inUseConnections ?? new ConcurrentHashSet<IPooledConnection>();
            if (validator != null)
            {
                _connectionValidator = validator;
            }
        }

        private async Task<IPooledConnection> CreateNewPooledConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = default(IPooledConnection);

            try
            {
                conn = NewPooledConnection();

                if (conn == null)
                    return null;

                await conn
                    .InitAsync(cancellationToken)
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

        private IPooledConnection NewPooledConnection()
        {
            if (!TryIncrementPoolSize())
                return null;

            _poolMetricsListener?.ConnectionCreating();

            return _connectionFactory.Create(_uri, this, RoutingContext);
        }

        private async Task DestroyConnectionAsync(IPooledConnection conn)
        {
            DecrementPoolSize();

            if (conn == null)
                return;

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
        /// Returns true if pool size is successfully increased, otherwise false.
        /// The reason to failed to increase the pool size probably due to the pool is full already
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
                return false;

            lock (_poolSizeSync)
            {
                if (PoolSize >= _maxPoolSize)
                    return false;

                Interlocked.Increment(ref _poolSize);
                return true;
            }
        }

        private void DecrementPoolSize()
        {
            Interlocked.Decrement(ref _poolSize);
        }

        public async Task<IConnection> AcquireAsync(AccessMode mode, string database, string impersonatedUser, Bookmarks bookmarks)
        {
            _poolMetricsListener?.PoolAcquiring();
            
            try
            {
                var connection = await TryExecuteAsync(
                        _logger, 
                        () => AcquireOrTimeoutAsync(mode, database, _connectionAcquisitionTimeout),
                        "Failed to acquire a connection from connection pool asynchronously.")
                    .ConfigureAwait(false);
                
                _poolMetricsListener?.PoolAcquired();
                return connection;
            }
            catch
            {
                _poolMetricsListener?.PoolFailedToAcquire();
                throw;
            }
        }

        private async Task<IPooledConnection> AcquireOrTimeoutAsync(AccessMode mode, string database, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                return await AcquireAsync(mode, database, cts.Token)
                    .Timeout(timeout, cts.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                _poolMetricsListener?.PoolTimedOutToAcquire();
                if (cts.Token.IsCancellationRequested)
                    throw new ClientException(
                        $"Failed to obtain a connection from pool within {_connectionAcquisitionTimeout}");

                throw new ClientException("Failed to obtain a connection from pool");
            }
        }

        private async Task<IPooledConnection> AcquireAsync(AccessMode mode, string database, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (IsClosed)
                    throw GetDriverDisposedException(nameof(ConnectionPool));

                if (IsInactive)
                    ThrowServerUnavailableExceptionDueToDeactivated();

                var connection = await GetPooledOrNewConnectionAsync(cancellationToken).ConfigureAwait(false);

                if (_connectionValidator.OnRequire(connection))
                {
                    await AddConnectionAsync(connection).ConfigureAwait(false);

                    connection.Mode = mode;
                    connection.Database = database;

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
                return;

            if (_inUseConnections.TryRemove(connection))
                await DestroyConnectionAsync(connection).ConfigureAwait(false);

            throw GetDriverDisposedException(nameof(ConnectionPool));
        }

        private Task<IPooledConnection> GetPooledOrNewConnectionAsync(CancellationToken cancellationToken)
        {
            return _idleConnections.TryTake(out var connection) 
                ? Task.FromResult(connection) 
                : CreateNewConnectionOrGetIdleAsync(cancellationToken);
        }

        private async Task<IPooledConnection> CreateNewConnectionOrGetIdleAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsConnectionPoolFull())
                {
                    var connection = await CreateNewPooledConnectionAsync(cancellationToken).ConfigureAwait(false);

                    if (connection != null)
                        return connection;
                }

                await Task.Delay(SpinningWaitInterval, cancellationToken).ConfigureAwait(false);

                if (_idleConnections.TryTake(out var idle))
                {
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

        public async Task ReleaseAsync(IPooledConnection connection)
        {
            await TryExecuteAsync(_logger, async () =>
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

                if (!await _connectionValidator.OnReleaseAsync(connection).ConfigureAwait(false)
                    || IsIdlePoolFull() || IsInactiveOrClosed)
                {
                    // cannot return to idle pool
                    await DestroyConnectionAsync(connection).ConfigureAwait(false);
                    return;
                }

                connection.Mode = null;
                connection.Database = null;

                // Add back to idle pool
                _idleConnections.Add(connection);
                // Just dequeue any one connection and close it will ensure that all connections in the pool will finally be closed
                if (IsInactiveOrClosed && _idleConnections.TryTake(out connection))
                {
                    await DestroyConnectionAsync(connection).ConfigureAwait(false);
                }
            }, $"Failed to release connection '{connection}' asynchronously back to pool.").ConfigureAwait(false);
        }

        public Task CloseAsync()
        {
            if (Interlocked.Exchange(ref _poolStatus, Closed) != Closed)
            {
                CloseAllConnectionsAsync();
            }

            return Task.CompletedTask;
        }
        
        public async Task VerifyConnectivityAsync()
        {
            // Establish a connection with the server and immediately close it.
            var connection = await AcquireAsync(Simple.Mode, Simple.Database, null, Simple.Bookmarks).ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);
        }

        public async Task<bool> SupportsMultiDbAsync()
        {
            // Establish a connection with the server and immediately close it.
            var connection = await AcquireAsync(Simple.Mode, Simple.Database, null, Simple.Bookmarks).ConfigureAwait(false);
            var multiDb = connection.SupportsMultidatabase();
            await connection.CloseAsync().ConfigureAwait(false);

            return multiDb;
        }

        public IRoutingTable GetRoutingTable(string database)
        {
            throw new NotSupportedException("Should not be getting a routing table on a connection pool when it is the connection provider to the driver. Only Loadbalancer should do that.");
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

        /// <summary>
        /// When a connection is marked as requiring reauthorization then all older connections also need to be marked in such a way.
        /// This will cause such marked connections to be closed and re-established with new authorization next time they are used.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public void MarkConnectionsForReauthorization(IPooledConnection connection)
        {
            var connectionAge = connection.LifetimeTimer.ElapsedMilliseconds;

            connection.ReAuthorizationRequired = true;

            foreach (var inUseConn in _inUseConnections)
            {
                if (inUseConn.LifetimeTimer.ElapsedMilliseconds >= connectionAge)
                {
                    inUseConn.ReAuthorizationRequired = true;
                }
            }
        }
    }
}