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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Throw.DriverDisposedException;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPool : LoggerBase, IConnectionPool, IConnectionProvider
    {
        private readonly Uri _uri;

        private readonly int _idleSessionPoolSize;
        private readonly ConnectionSettings _connectionSettings;

        private readonly ILogger _logger;

        private readonly ConcurrentQueue<IPooledConnection> _availableConnections = new ConcurrentQueue<IPooledConnection>();
        private readonly ConcurrentSet<IPooledConnection> _inUseConnections = new ConcurrentSet<IPooledConnection>();

        private volatile bool _disposeCalled;

        // for test only
        private readonly IConnection _fakeConnection;

        private readonly ConnectionPoolStatistics _statistics;

        internal int NumberOfInUseConnections => _inUseConnections.Count;
        internal int NumberOfAvailableConnections => _availableConnections.Count;

        internal bool DisposeCalled
        {
            set { _disposeCalled = value; }
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
            _idleSessionPoolSize = connectionPoolSettings.MaxIdleSessionPoolSize;

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
            ConcurrentQueue<IPooledConnection> availableConnections = null,
            ConcurrentSet<IPooledConnection> inUseConnections = null,
            ILogger logger = null,
            ConnectionPoolSettings settings = null)
            : this(null, null, settings ?? new ConnectionPoolSettings(Config.DefaultConfig), 
                  logger)
        {
            _fakeConnection = connection;
            _availableConnections = availableConnections ?? new ConcurrentQueue<IPooledConnection>();
            _inUseConnections = inUseConnections ?? new ConcurrentSet<IPooledConnection>();
        }

        public IPooledConnection CreateNewPooledConnection()
        {
            PooledConnection conn = null;
            try
            {
                _statistics?.IncrementConnectionToCreate();

                conn = _fakeConnection != null
                    ? new PooledConnection(_fakeConnection, Release)
                    : new PooledConnection(new SocketConnection(_uri, _connectionSettings, _logger), Release);
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
                    CloseConnection(conn);
                }
                throw;
            }
        }

        private void CloseConnection(IPooledConnection conn)
        {
            _statistics?.IncrementConnectionToClose();

            conn.Close();

            _statistics?.IncrementConnectionClosed();
        }

        public IConnection Acquire(AccessMode mode)
        {
            return Acquire();
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

                if (!_availableConnections.TryDequeue(out connection))
                {
                    connection = CreateNewPooledConnection();
                }
                else if (!connection.IsOpen)
                {
                    CloseConnection(connection);
                    return Acquire();
                }

                _inUseConnections.TryAdd(connection);
                if (_disposeCalled)
                {
                    if (_inUseConnections.TryRemove(connection))
                    {
                        CloseConnection(connection);
                    }
                    ThrowObjectDisposedException();
                }

                return connection;
            });
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

        private bool IsPoolFull()
        {
            return _availableConnections.Count >= _idleSessionPoolSize && _idleSessionPoolSize != Config.InfiniteMaxIdleSessionPoolSize;
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
                        CloseConnection(connection);
                    }
                    else
                    {
                        _availableConnections.Enqueue(connection);
                    }

                    // Just dequeue any one connection and close it will ensure that all connections in the pool will finally be closed
                    if (_disposeCalled && _availableConnections.TryDequeue(out connection))
                    {
                        CloseConnection(connection);
                    }
                }
                else
                {
                    //release resources by connection
                    CloseConnection(connection);
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
                        CloseConnection(inUseConnection);
                    }
                }

                IPooledConnection connection;
                while (_availableConnections.TryDequeue(out connection))
                {
                    Logger?.Debug($"Disposing Available Connection {connection.Id}");
                    CloseConnection(connection);
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

    internal interface IConnectionPool : IDisposable
    {
        IPooledConnection Acquire();
        void Release(IPooledConnection connection);
    }
}