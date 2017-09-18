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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Throw.DriverDisposedException;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPool : LoggerBase, IConnectionPool
    {
        private const int SpinningWaitInterval = 500;

        private readonly Uri _uri;
        private readonly object _syncObject = new object();

        private int _poolSize = 0;
        private readonly int _maxPoolSize;
        private readonly int _idlePoolSize;
        private readonly TimeSpan _connAcquisitionTimeout;

        private readonly ConnectionValidator _connectionValidator;
        private readonly ConnectionSettings _connectionSettings;
        private readonly BufferSettings _bufferSettings;

        private readonly BlockingCollection<IPooledConnection> _availableConnections = new BlockingCollection<IPooledConnection>();
        private readonly ConcurrentSet<IPooledConnection> _inUseConnections = new ConcurrentSet<IPooledConnection>();

        private volatile bool _disposeCalled;

        // for test only
        private readonly IConnection _fakeConnection;

        private IStatisticsCollector _statisticsCollector;
        private ConnectionPoolStatistics _statistics;

        public int NumberOfInUseConnections => _inUseConnections.Count;
        internal int NumberOfAvailableConnections => _availableConnections.Count;

        internal bool DisposeCalled
        {
            set => _disposeCalled = value;
        }

        public ConnectionPool(
            Uri uri,
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings connectionPoolSettings,
            BufferSettings bufferSettings,
            ILogger logger)
            : base(logger)
        {
            _uri = uri;
            _connectionSettings = connectionSettings;

            _maxPoolSize = connectionPoolSettings.MaxConnectionPoolSize;
            _idlePoolSize = connectionPoolSettings.MaxIdleConnectionPoolSize;
            _connAcquisitionTimeout = connectionPoolSettings.ConnectionAcquisitionTimeout;
            _bufferSettings = bufferSettings;


            var connIdleTimeout = connectionPoolSettings.ConnectionIdleTimeout;
            var maxConnectionLifetime = connectionPoolSettings.MaxConnectionLifetime;
            _connectionValidator = new ConnectionValidator(connIdleTimeout, maxConnectionLifetime);

            SetupStatisticsProvider(connectionPoolSettings.StatisticsCollector);
        }

        internal ConnectionPool(
            IConnection connection,
            BlockingCollection<IPooledConnection> availableConnections = null,
            ConcurrentSet<IPooledConnection> inUseConnections = null,
            ILogger logger = null,
            ConnectionPoolSettings settings = null,
            BufferSettings bufferSettings = null)
            : this(null, null, settings ?? new ConnectionPoolSettings(Config.DefaultConfig), 
                  bufferSettings ?? new BufferSettings(Config.DefaultConfig), logger)
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

                conn = NewPooledConnection();
                conn.Init();

                _statistics?.IncrementConnectionCreated();
                return conn;
            }
            catch
            {
                _statistics?.IncrementConnectionFailedToCreate();

                // shut down and clean all the resources of the conneciton if failed to establish
                DestoryConnection(conn);
                throw;
            }
        }

        private async Task<IPooledConnection> CreateNewPooledConnectionAsync()
        {
            PooledConnection conn = null;
            try
            {
                _statistics?.IncrementConnectionToCreate();

                conn = NewPooledConnection();
                await conn.InitAsync().ConfigureAwait(false);

                _statistics?.IncrementConnectionCreated();
                return conn;
            }
            catch
            {
                _statistics?.IncrementConnectionFailedToCreate();

                // shut down and clean all the resources of the conneciton if failed to establish
                DestoryConnection(conn);
                throw;
            }
        }

        private PooledConnection NewPooledConnection()
        {
            Interlocked.Increment(ref _poolSize);
            if (_maxPoolSize != Config.Infinite && _poolSize > _maxPoolSize)
            {
                lock (_syncObject)
                {
                    if (_poolSize > _maxPoolSize)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            return _fakeConnection != null
                ? new PooledConnection(_fakeConnection, this)
                : new PooledConnection(new SocketConnection(_uri, _connectionSettings, _bufferSettings, Logger), this);
        }

        private void DestoryConnection(IPooledConnection conn)
        {
            Interlocked.Decrement(ref _poolSize);
            if (conn == null)
            {
                return;
            }

            _statistics?.IncrementConnectionToClose();
            conn.Destroy();
            _statistics?.IncrementConnectionClosed();
        }

        public IConnection Acquire(AccessMode mode)
        {
            return Acquire();
        }

        public IPooledConnection Acquire()
        {
            using (var timeOutTokenSource = new CancellationTokenSource(_connAcquisitionTimeout))
            {
                return Acquire(timeOutTokenSource.Token);
            }
        }

        private IPooledConnection Acquire(CancellationToken cancellationToken)
        {
            return TryExecute(() =>
            {
                IPooledConnection connection = null;

                try
                {
                    while (true)
                    {
                        if (_disposeCalled)
                        {
                            ThrowObjectDisposedException();
                        }

                        if (!_availableConnections.TryTake(out connection))
                        {
                            do
                            {
                                if (!IsConnectionPoolFull())
                                {
                                    try
                                    {
                                        connection = CreateNewPooledConnection();
                                        break;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // we have exceeded MaxConnectionPoolSize, so continue looping...
                                    }
                                }

                                if (_availableConnections.TryTake(out connection, SpinningWaitInterval, cancellationToken))
                                {
                                    break;
                                }
                            } while (!cancellationToken.IsCancellationRequested);

                            if (connection == null)
                            {
                                throw new ClientException(
                                    $"Failed to obtain a connection from pool within {_connAcquisitionTimeout}");
                            }
                        }

                        if (!_connectionValidator.IsValid(connection))
                        {
                            DestoryConnection(connection);
                        }
                        else
                        {
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();
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
                }
                catch (OperationCanceledException ex)
                {
                    throw new ClientException(
                        $"Failed to obtain a connection from pool within {_connAcquisitionTimeout}", ex);
                }

                return connection;
            });
        }

        public Task<IConnection> AcquireAsync(AccessMode mode)
        {
            using (var timeOutTokenSource = new CancellationTokenSource(_connAcquisitionTimeout))
            {
                return AcquireAsync(timeOutTokenSource.Token);
            }
        }

        private Task<IConnection> AcquireAsync(CancellationToken cancellationToken)
        {
            return TryExecuteAsync(async () =>
            {
                IPooledConnection connection = null;

                try
                {
                    while (true)
                    {
                        if (_disposeCalled)
                        {
                            ThrowObjectDisposedException();
                        }

                        if (!_availableConnections.TryTake(out connection))
                        {
                            do
                            {
                                if (!IsConnectionPoolFull())
                                {
                                    try
                                    {
                                        connection = await CreateNewPooledConnectionAsync().ConfigureAwait(false);
                                        break;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // we have exceeded MaxConnectionPoolSize, so continue looping...
                                    }
                                }

                                if (_availableConnections.TryTake(out connection, SpinningWaitInterval, cancellationToken))
                                {
                                    break;
                                }
                            } while (!cancellationToken.IsCancellationRequested);

                            if (connection == null)
                            {
                                throw new ClientException(
                                    $"Failed to obtain a connection from pool within {_connAcquisitionTimeout}");
                            }
                        }

                        if (!_connectionValidator.IsValid(connection))
                        {
                            DestoryConnection(connection);
                        }
                        else
                        {
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();
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
                }
                catch (OperationCanceledException ex)
                {
                    throw new ClientException(
                        $"Failed to obtain a connection from pool within {_connAcquisitionTimeout}", ex);
                }

                return (IConnection) connection;
            });
        }

        private bool IsConnectionPoolFull()
        {
            return _maxPoolSize != Config.Infinite && _poolSize >= _maxPoolSize;
        }

        private bool IsIdlePoolFull()
        {
            return _availableConnections.Count >= _idlePoolSize;
        }

        public void Release(IPooledConnection connection)
        {
            TryExecute(() =>
            {
                if (_disposeCalled)
                {
                    // pool already disposed.
                    return;
                }
                if (!_inUseConnections.TryRemove(connection))
                {
                    // pool already disposed.
                    return;
                }
                if (_connectionValidator.IsConnectionReusable(connection))
                {
                    if (IsIdlePoolFull())
                    {
                        DestoryConnection(connection);
                    }
                    else
                    {
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

                if (await _connectionValidator.IsConnectionReusableAsync(connection).ConfigureAwait(false))
                {
                    if (IsIdlePoolFull())
                    {
                        DestoryConnection(connection);
                    }
                    else
                    {
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
            DisposeStatisticsProvider();
            base.Dispose(true);
        }

        private void ThrowObjectDisposedException()
        {
            FailedToCreateConnection(this);
        }

        private void SetupStatisticsProvider(IStatisticsCollector collector)
        {
            _statisticsCollector = collector;
            if (_statisticsCollector != null)
            {
                _statistics = new ConnectionPoolStatistics(_uri, this);
                _statisticsCollector.Register(_statistics);
            }
        }

        private void DisposeStatisticsProvider()
        {
            if (_statistics != null)
            {
                _statisticsCollector?.Unregister(_statistics);
                _statistics.Dispose();
            }
        }

        public override string ToString()
        {
            return $"{nameof(_availableConnections)}: {{{_availableConnections.ValueToString()}}}, " +
                   $"{nameof(_inUseConnections)}: {{{_inUseConnections}}}";
        }
    }
}
