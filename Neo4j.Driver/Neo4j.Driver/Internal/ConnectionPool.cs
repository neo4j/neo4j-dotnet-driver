// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.ExponentialBackoffRetryLogic;
using static Neo4j.Driver.Internal.Throw.DriverDisposedException;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPool : LoggerBase, IConnectionReleaseManager, IConnectionProvider
    {
        private readonly Uri _uri;

        private int _poolSize = 0;
        private readonly int _maxPoolSize;
        private readonly int _idlePoolSize;
        private readonly TimeSpan _connIdleTimeout;
        private readonly TimeSpan _connAcquisitionTimeout;

        private readonly ConnectionSettings _connectionSettings;

        private readonly ILogger _logger;

        private readonly BlockingCollection<IPooledConnection> _availableConnections = new BlockingCollection<IPooledConnection>();
        private readonly ConcurrentSet<IPooledConnection> _inUseConnections = new ConcurrentSet<IPooledConnection>();

        private volatile bool _disposeCalled;

        // for test only
        private readonly IConnection _fakeConnection;

        private readonly ConnectionPoolStatistics _statistics;

        internal int NumberOfInUseConnections => _inUseConnections.Count;
        internal int NumberOfAvailableConnections => _availableConnections.Count;

        internal bool DisposeCalled
        {
            set => _disposeCalled = value;
        }

        public ConnectionPool(
            Uri uri,
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings connectionPoolSettings,
            ILogger logger)
            : base(logger)
        {
            _uri = uri;
            _connectionSettings = connectionSettings;
            _maxPoolSize = connectionPoolSettings.MaxConnectionPoolSize;
            _idlePoolSize = connectionPoolSettings.MaxIdleConnectionPoolSize;
            _connIdleTimeout = connectionPoolSettings.ConnectionIdleTimeout;
            _connAcquisitionTimeout = connectionPoolSettings.ConnectionAcquisitionTimeout;

            _logger = logger;

            var statisticsCollector = connectionPoolSettings.StatisticsCollector;
            if (statisticsCollector != null)
            {
                _statistics = new ConnectionPoolStatistics(this);
                statisticsCollector.Register(_statistics);
            }
        }

        internal ConnectionPool(
            IConnection connection,
            BlockingCollection<IPooledConnection> availableConnections = null,
            ConcurrentSet<IPooledConnection> inUseConnections = null,
            ILogger logger = null,
            ConnectionPoolSettings settings = null)
            : this(null, null, settings ?? new ConnectionPoolSettings(Config.DefaultConfig), 
                  logger)
        {
            _fakeConnection = connection;
            _availableConnections = availableConnections ?? new BlockingCollection<IPooledConnection>();
            _inUseConnections = inUseConnections ?? new ConcurrentSet<IPooledConnection>();
        }

        public IPooledConnection CreateNewPooledConnection()
        {
            PooledConnection conn = null;
            try
            {
                _statistics?.IncrementConnectionToCreate();
                Interlocked.Increment(ref _poolSize);

                conn = _fakeConnection != null
                    ? new PooledConnection(_fakeConnection, this)
                    : new PooledConnection(new SocketConnection(_uri, _connectionSettings, _logger), this);
                conn.Init();

                _statistics?.IncrementConnectionCreated();
                return conn;
            }
            catch
            {
                _statistics?.IncrementConnectionFailedToCreate();

                // shut down and clean all the resources of the conneciton if failed to establish
                if (conn != null)
                {
                    DestoryConnection(conn);
                }
                else
                {
                    Interlocked.Decrement(ref _poolSize);
                }
                throw;
            }
        }

        private async Task<IPooledConnection> CreateNewPooledConnectionAsync()
        {
            PooledConnection conn = null;
            try
            {
                _statistics?.IncrementConnectionToCreate();
                Interlocked.Increment(ref _poolSize);

                conn = _fakeConnection != null
                    ? new PooledConnection(_fakeConnection, this)
                    : new PooledConnection(new SocketConnection(_uri, _connectionSettings, _logger), this);
                await conn.InitAsync().ConfigureAwait(false);

                _statistics?.IncrementConnectionCreated();
                return conn;
            }
            catch
            {
                _statistics?.IncrementConnectionFailedToCreate();

                // shut down and clean all the resources of the conneciton if failed to establish
                if (conn != null)
                {
                    DestoryConnection(conn);
                }
                else
                {
                    Interlocked.Decrement(ref _poolSize);
                }
                throw;
            }
        }

        private void DestoryConnection(IPooledConnection conn)
        {
            Interlocked.Decrement(ref _poolSize);
            _statistics?.IncrementConnectionToClose();

            conn.Destroy();

            _statistics?.IncrementConnectionClosed();
        }

        public IConnection Acquire(AccessMode mode)
        {
            return Acquire();
        }

        public Task<IConnection> AcquireAsync(AccessMode mode)
        {
            return AcquireAsync();
        }

        public IPooledConnection Acquire()
        {
            return TryExecute(() =>
            {
                if (_disposeCalled)
                {
                    ThrowObjectDisposedException();
                }
                IPooledConnection connection;

                if (!_availableConnections.TryTake(out connection))
                {
                    connection = CreateNewPooledConnection();
                }
                else if (!connection.IsOpen || HasBeenIdleForTooLong(connection))
                {
                    DestoryConnection(connection);
                    return Acquire();
                }

                _inUseConnections.TryAdd(connection);
                if (_disposeCalled)
                {
                    if (_inUseConnections.TryRemove(connection))
                    {
                        DestoryConnection(connection);
                    }
                    ThrowObjectDisposedException();
                }

                return connection;
            });
        }

        private Task<IConnection> AcquireAsync()
        {
            return TryExecuteAsync(async () =>
            {
                if (_disposeCalled)
                {
                    ThrowObjectDisposedException();
                }

                IPooledConnection connection = null;
                if (!_availableConnections.TryTake(out connection))
                {
                    // TODO: make this timer a cancellationToken instead
                    Stopwatch connAcquisitionTimer = new Stopwatch();
                    connAcquisitionTimer.Start();
                    do
                    {
                        if (_poolSize < _maxPoolSize)
                        {
                            connection = await CreateNewPooledConnectionAsync().ConfigureAwait(false);
                            break;
                        }
                        // The pool is full at this moment
                        if (_availableConnections.TryTake(out connection, TimeSpan.FromSeconds(5)))
                        {
                            break;
                        }
                    } while (connAcquisitionTimer.ElapsedMilliseconds < _connAcquisitionTimeout.TotalMilliseconds);
                    connAcquisitionTimer.Stop();

                    if (connection == null)
                    {
                        throw new ClientException($"Failed to obtain a connection from pool within {_connAcquisitionTimeout}");
                    }
                }

                if (!connection.IsOpen || HasBeenIdleForTooLong(connection))
                {
                    DestoryConnection(connection);
                    return await AcquireAsync().ConfigureAwait(false);
                }

                _inUseConnections.TryAdd(connection);
                if (_disposeCalled)
                {
                    if (_inUseConnections.TryRemove(connection))
                    {
                        DestoryConnection(connection);
                    }
                    ThrowObjectDisposedException();
                }

                return connection;
            });
        }

        private bool IsConnectionIdleDetectionEnabled()
        {
            return _connIdleTimeout.TotalMilliseconds >= 0;
        }

        private bool HasBeenIdleForTooLong(IPooledConnection connection)
        {
            if (!IsConnectionIdleDetectionEnabled())
            {
                return false;
            }
            if (connection.IdleTimer.ElapsedMilliseconds > _connIdleTimeout.TotalMilliseconds)
            {
                return true;
            }
            connection.IdleTimer.Reset();
            return false;
        }

        private bool IsConnectionReusable(IPooledConnection connection)
        {
            if (!connection.IsOpen)
            {
                return false;
            }

            try
            {
                connection.ClearConnection();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private async Task<bool> IsConnectionReusableAsync(IPooledConnection connection)
        {
            if (!connection.IsOpen)
            {
                return false;
            }

            try
            {
                await connection.ClearConnectionAsync().ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool IsPoolFull()
        {
            return _availableConnections.Count >= _idlePoolSize;
        }

        public void Release(IPooledConnection connection)
        {
            TryExecute(() =>
            {
                if (_disposeCalled)
                {
                    // pool already disposed
                    return;
                }
                if (!_inUseConnections.TryRemove(connection))
                {
                    // pool already disposed
                    return;
                }

                if (IsConnectionReusable(connection))
                {
                    if (IsPoolFull())
                    {
                        DestoryConnection(connection);
                    }
                    else
                    {
                        if (IsConnectionIdleDetectionEnabled())
                        {
                            connection.IdleTimer.Start();
                        }
                        _availableConnections.Add(connection);
                    }

                    // Just dequeue any one connection and close it will ensure that all connections in the pool will finally be closed
                    if (_disposeCalled && _availableConnections.TryTake(out connection))
                    {
                        DestoryConnection(connection);
                    }
                }
                else
                {
                    //release resources by connection
                    DestoryConnection(connection);
                }
            });
        }

        public Task ReleaseAsync(IPooledConnection connection)
        {
            return TryExecuteAsync(async () =>
            {
                if (_disposeCalled)
                {
                    // pool already disposed
                    return;
                }
                if (!_inUseConnections.TryRemove(connection))
                {
                    // pool already disposed
                    return;
                }

                if (await IsConnectionReusableAsync(connection).ConfigureAwait(false))
                {
                    if (IsPoolFull())
                    {
                        DestoryConnection(connection);
                    }
                    else
                    {
                        if (IsConnectionIdleDetectionEnabled())
                        {
                            connection.IdleTimer.Start();
                        }
                        _availableConnections.Add(connection);
                    }

                    // Just dequeue any one connection and close it will ensure that all connections in the pool will finally be closed
                    if (_disposeCalled && _availableConnections.TryTake(out connection))
                    {
                        DestoryConnection(connection);
                    }
                }
                else
                {
                    //release resources by connection
                    DestoryConnection(connection);
                }
            });
        }

        // For concurrent calling: you are free to get something from inUseConn or availConn when we dispose.
        // However it is forbiden to put something back to the conn queues after we've already started disposing.
        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            TryExecute(() =>
            {
                _disposeCalled = true;
                foreach (var inUseConnection in _inUseConnections)
                {
                    Logger?.Info($"Disposing In Use Connection {inUseConnection.Id}");
                    if (_inUseConnections.TryRemove(inUseConnection))
                    {
                        DestoryConnection(inUseConnection);
                    }
                }

                IPooledConnection connection;
                while (_availableConnections.TryTake(out connection))
                {
                    Logger?.Debug($"Disposing Available Connection {connection.Id}");
                    DestoryConnection(connection);
                }
            });
            _statistics?.Dispose();
            base.Dispose(true);
        }

        private void ThrowObjectDisposedException()
        {
            FailedToCreateConnection(this);
        }

        public override string ToString()
        {
            return $"{nameof(_availableConnections)}: {{{_availableConnections.ValueToString()}}}, " +
                   $"{nameof(_inUseConnections)}: {{{_inUseConnections}}}";
        }
    }

    internal interface IConnectionReleaseManager : IDisposable
    {
        void Release(IPooledConnection connection);
        Task ReleaseAsync(IPooledConnection connection);
    }
}