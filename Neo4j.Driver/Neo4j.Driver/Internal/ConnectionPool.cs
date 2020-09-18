// Copyright (c) 2002-2020 "Neo4j,"
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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;
using static Neo4j.Driver.Internal.ConnectionPoolStatus;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;
using static Neo4j.Driver.Internal.Throw.ObjectDisposedException;
using static Neo4j.Driver.Internal.Util.ConnectionContext;

namespace Neo4j.Driver.Internal
{
    internal sealed class ConnectionPoolStatus
    {
        public static readonly ConnectionPoolStatus Active = new ConnectionPoolStatus(PoolStatus.Open);
        public static readonly ConnectionPoolStatus Closed = new ConnectionPoolStatus(PoolStatus.Closed);
        public static readonly ConnectionPoolStatus Inactive = new ConnectionPoolStatus(PoolStatus.Inactive);

        private readonly PoolStatus _code;

        private ConnectionPoolStatus(PoolStatus code)
        {
            _code = code;
        }
    }

    internal enum PoolStatus
    {
        Open,
        Closed,
        Inactive
    }

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
        private readonly TimeSpan _connAcquisitionTimeout;

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
            _connAcquisitionTimeout = connectionPoolSettings.ConnectionAcquisitionTimeout;
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

        private async Task<IPooledConnection> CreateNewPooledConnectionAsync()
        {
            IPooledConnection conn = null;
            try
            {
                conn = NewPooledConnection();
                if (conn != null)
                {
                    await conn.InitAsync().ConfigureAwait(false);
                    _poolMetricsListener?.ConnectionCreated();
                    return conn;
                }
            }
            catch
            {
                _poolMetricsListener?.ConnectionFailedToCreate();

                // shut down and clean all the resources of the connection if failed to establish
                await DestroyConnectionAsync(conn).ConfigureAwait(false);
                throw;
            }

            return null;
        }

        private IPooledConnection NewPooledConnection()
        {
            if (TryIncrementPoolSize())
            {
                _poolMetricsListener?.ConnectionCreating();

                return _connectionFactory.Create(_uri, this, RoutingContext);
            }

            return null;
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

            if (PoolSize < _maxPoolSize)
            {
                lock (_poolSizeSync)
                {
                    if (PoolSize < _maxPoolSize)
                    {
                        Interlocked.Increment(ref _poolSize);

                        return true;
                    }
                }
            }

            return false;
        }

        private void DecrementPoolSize()
        {
            Interlocked.Decrement(ref _poolSize);
        }

        private void ThrowConnectionAcquisitionTimedOutException(OperationCanceledException ex = null)
        {
            _poolMetricsListener?.PoolTimedOutToAcquire();
            throw new ClientException(
                $"Failed to obtain a connection from pool within {_connAcquisitionTimeout}", ex);
        }

        public Task<IConnection> AcquireAsync(AccessMode mode, string database, Bookmark bookmark)
        {
            _poolMetricsListener?.PoolAcquiring();
            var timeOutTokenSource = new CancellationTokenSource(_connAcquisitionTimeout);
            var task = AcquireAsync(mode, database, timeOutTokenSource.Token).ContinueWith(t =>
            {
                timeOutTokenSource.Dispose();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    _poolMetricsListener?.PoolAcquired();
                }
                else
                {
                    _poolMetricsListener?.PoolFailedToAcquire();
                }

                return t;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
            return task;
        }

        private Task<IConnection> AcquireAsync(AccessMode mode, string database, CancellationToken cancellationToken)
        {
            return TryExecuteAsync(_logger, async () =>
            {
                IPooledConnection connection = null;
                try
                {
                    while (true)
                    {
                        if (IsClosed)
                        {
                            ThrowObjectDisposedException();
                        }
                        else if (IsInactive)
                        {
                            ThrowClientExceptionDueToDeactivated();
                        }

                        if (!_idleConnections.TryTake(out connection))
                        {
                            do
                            {
                                if (!IsConnectionPoolFull())
                                {
                                    connection = await CreateNewPooledConnectionAsync().ConfigureAwait(false);
                                    if (connection != null)
                                    {
                                        break;
                                    }
                                }

                                await Task.Delay(SpinningWaitInterval, cancellationToken).ConfigureAwait(false);

                                if (_idleConnections.TryTake(out connection))
                                {
                                    break;
                                }
                            } while (!cancellationToken.IsCancellationRequested);

                            if (connection == null)
                            {
                                ThrowConnectionAcquisitionTimedOutException();
                            }
                        }

                        if (!_connectionValidator.OnRequire(connection))
                        {
                            await DestroyConnectionAsync(connection).ConfigureAwait(false);
                        }
                        else
                        {
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    _inUseConnections.TryAdd(connection);
                    if (IsClosed)
                    {
                        if (_inUseConnections.TryRemove(connection))
                        {
                            await DestroyConnectionAsync(connection).ConfigureAwait(false);
                        }

                        ThrowObjectDisposedException();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    ThrowConnectionAcquisitionTimedOutException(ex);
                }

                if (connection != null)
                {
                    connection.Mode = mode;
                    connection.Database = database;
                }

                return (IConnection) connection;
            }, "Failed to acquire a connection from connection pool asynchronously.");
        }

        private bool IsConnectionPoolFull()
        {
            return _maxPoolSize != Config.Infinite && _poolSize >= _maxPoolSize;
        }

        private bool IsIdlePoolFull()
        {
            return _maxIdlePoolSize != Config.Infinite && _idleConnections.Count >= _maxIdlePoolSize;
        }

        public Task ReleaseAsync(IPooledConnection connection)
        {
            return TryExecuteAsync(_logger, async () =>
            {
                if (IsClosed)
                {
                    // pool already disposed
                    return;
                }

                // Remove from idle
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
            }, $"Failed to release connection '{connection}' asynchronously back to pool.");
        }

        public Task CloseAsync()
        {
            if (Interlocked.Exchange(ref _poolStatus, Closed) != Closed)
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

                _poolMetricsListener?.Dispose();
                return Task.WhenAll(allCloseTasks);
            }

            return Task.CompletedTask;
        }

        public async Task VerifyConnectivityAsync()
        {
            // Establish a connection with the server and immediately close it.
            var connection = await AcquireAsync(Simple.Mode, Simple.Database, Simple.Bookmark).ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);
        }

        public async Task<bool> SupportsMultiDbAsync()
        {
            // Establish a connection with the server and immediately close it.
            var connection = await AcquireAsync(Simple.Mode, Simple.Database, Simple.Bookmark).ConfigureAwait(false);
            var multiDb = connection.SupportsMultidatabase();
            await connection.CloseAsync().ConfigureAwait(false);

            return multiDb;
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

        private void ThrowObjectDisposedException()
        {
            FailedToAcquireConnectionDueToPoolClosed(this);
        }

        private void ThrowClientExceptionDueToDeactivated()
        {
            throw new ClientException(
                $"Failed to acquire a connection from connection pool for server with URI `{_uri}` " +
                "as this server has already been removed from routing table. " +
                "Please retry your query again and you should be routed with a different server from the new routing table. " +
                "You should not see this error persistently.");
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
    }
}