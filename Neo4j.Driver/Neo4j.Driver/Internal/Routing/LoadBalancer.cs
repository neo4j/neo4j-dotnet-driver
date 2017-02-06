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
using System.Diagnostics;
using System.Linq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class LoadBalancer : ILoadBalancer
    {
        private IRoutingTable _routingTable;
        private readonly IClusterConnectionPool _clusterConnectionPool;
        private readonly ILogger _logger;
        private readonly object _syncLock = new object();
        private readonly Stopwatch _stopwatch;

        public LoadBalancer(
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            _clusterConnectionPool = new ClusterConnectionPool(
                connectionSettings, poolSettings, logger, CreateClusterPooledConnectionErrorHandler);

            _stopwatch = new Stopwatch();
            _routingTable = new RoundRobinRoutingTable(connectionSettings.InitialServerUri, _stopwatch);

            _logger = logger;
        }

        // for test only
        internal LoadBalancer(
            IClusterConnectionPool clusterConnPool,
            IRoutingTable routingTable)
        {
            _clusterConnectionPool = clusterConnPool;
            _routingTable = routingTable;
            _logger = null;
        }

        public IPooledConnection AcquireConnection(AccessMode mode)
        {
            EnsureRoutingTableIsFresh();
            switch (mode)
            {
                case AccessMode.Read:
                    return AcquireReadConnection();
                case AccessMode.Write:
                    return AcquireWriteConnection();
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }
        }

        internal IPooledConnection AcquireReadConnection()
        {
            while (true)
            {
                Uri uri;
                if (!_routingTable.TryNextReader(out uri))
                {
                    // no server known to routingTable
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_clusterConnectionPool.TryAcquire(uri, out conn))
                    {
                        return conn;
                    }
                }
                catch (SessionExpiredException)
                {
                    // ignored
                    // Already handled by connectionpool error handler to remove from load balancer
                }
            }
            throw new SessionExpiredException("Failed to connect to any read server.");
        }

        internal IPooledConnection AcquireWriteConnection()
        {
            while(true)
            {
                Uri uri;
                if (!_routingTable.TryNextWriter(out uri))
                {
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_clusterConnectionPool.TryAcquire(uri, out conn))
                    {
                        return conn;
                    }
                }
                catch (SessionExpiredException)
                {
                    // ignored
                    // Already handled by connectionpool error handler to remove from load balancer
                }
            }
            throw new SessionExpiredException("Failed to connect to any write server.");
        }

        public void Forget(Uri uri)
        {
            _routingTable.Remove(uri);
            _clusterConnectionPool.Purge(uri);
        }

        internal void EnsureRoutingTableIsFresh()
        {
            lock (_syncLock)
            {
                if (!_routingTable.IsStale())
                {
                    return;
                }

                var routingTable = UpdateRoutingTable();
                _clusterConnectionPool.Update(routingTable.All());
                _routingTable = routingTable;
                _logger.Info($"Updated routingTable to be {_routingTable}");
            }
        }

        internal IRoutingTable UpdateRoutingTable(Func<IPooledConnection, IRoutingTable> rediscoveryFunc = null)
        {
            lock (_syncLock)
            {
                while (true)
                {
                    Uri uri;
                    if (!_routingTable.TryNextRouter(out uri))
                    {
                        // no alive server
                        break;
                    }
                    try
                    {
                        IPooledConnection conn;
                        if (_clusterConnectionPool.TryAcquire(uri, out conn))
                        {
                            var roundRobinRoutingTable = rediscoveryFunc == null ? Rediscovery(conn) : rediscoveryFunc.Invoke(conn);
                            if (!roundRobinRoutingTable.IsStale())
                            {
                                return roundRobinRoutingTable;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        _logger?.Info($"Failed to update routing table with server uri={uri} due to error {e.Message}");
                        if (e is SessionExpiredException)
                        {
                            // ignored
                            // As already handled by connection pool error handler to remove from load balancer
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                // We retied and tried our best however there is just no cluster.
                // This is the ultimate place we will inform the user that you need to re-create a driver
                throw new ServiceUnavailableException(
                    "Failed to connect to any routing server. " +
                    "Please make sure that the cluster is up and can be accessed by the driver and retry.");
            }
        }

        private IRoutingTable Rediscovery(IPooledConnection conn)
        {
            var discoveryManager = new ClusterDiscoveryManager(conn, _logger);
            discoveryManager.Rediscovery();
            return new RoundRobinRoutingTable(discoveryManager.Routers, discoveryManager.Readers,
                discoveryManager.Writers, _stopwatch, discoveryManager.ExpireAfterSeconds);
        }

        private Exception OnConnectionError(Exception e, Uri uri)
        {
            if (e is ServiceUnavailableException)
            {
                _logger?.Info($"Server at {uri} is no longer available due to error: {e.Message}.");
                Forget(uri);
                return new SessionExpiredException($"Server at {uri} is no longer available due to error: {e.Message}.", e);
            }
            return e;
        }

        private Neo4jException OnServerError(Neo4jException error, Uri uri)
        {
            if (error.IsClusterNotALeaderError())
            {
                // The lead is no longer a leader, a.k.a. the write server no longer accepts writes
                // However the server is still available for possible reads.
                // Therefore we just remove it from ClusterView but keep it in connection pool.
                _routingTable.Remove(uri);
                return new SessionExpiredException($"Server at {uri} no longer accepts writes");
            }
            else if (error.IsForbiddenOnReadOnlyDatabaseError())
            {
                // The user was trying to run a write in a read session
                // So inform the user and let him try with a proper session mode
                return new ClientException("Write queries cannot be performed in READ access mode.");
            }
            return error;
        }

        internal ClusterPooledConnectionErrorHandler CreateClusterPooledConnectionErrorHandler(Uri uri)
        {
            return new ClusterPooledConnectionErrorHandler(x => OnConnectionError(x, uri), x => OnServerError(x, uri));
        }

        internal class ClusterPooledConnectionErrorHandler : IConnectionErrorHandler
        {
            private Func<Exception, Exception> _onConnectionErrorFunc;
            private readonly Func<Neo4jException, Neo4jException> _onNeo4jErrorFunc;

            public ClusterPooledConnectionErrorHandler(Func<Exception, Exception> onConnectionErrorFuncFunc, Func<Neo4jException, Neo4jException> onNeo4JErrorFuncFunc)
            {
                _onConnectionErrorFunc = onConnectionErrorFuncFunc;
                _onNeo4jErrorFunc = onNeo4JErrorFuncFunc;
            }

            public Exception OnConnectionError(Exception e)
            {
                return _onConnectionErrorFunc.Invoke(e);
            }

            public Neo4jException OnServerError(Neo4jException e)
            {
                return _onNeo4jErrorFunc.Invoke(e);
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            // We cannot set routing table and cluster conn pool to null as we do not want get NPE in concurrent call of dispose and acquire
            _routingTable.Clear();
            _clusterConnectionPool.Dispose();

            // cannot set logger to null here otherwise we might concurrent call log and set log to null.
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}