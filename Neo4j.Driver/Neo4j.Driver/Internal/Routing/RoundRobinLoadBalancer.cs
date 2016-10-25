// Copyright (c) 2002-2016 "Neo Technology,"
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoundRobinLoadBalancer : ILoadBalancer
    {
        private RoundRobinClusterView _clusterView;
        private IClusterConnectionPool _connectionPool;
        private ILogger _logger;
        private readonly object _syncLock = new object();

        public RoundRobinLoadBalancer(
            Uri seedServer,
            IAuthToken authToken,
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            _connectionPool = new ClusterConnectionPool(seedServer, authToken, encryptionManager, poolSettings, logger, CreateClusterPooledConnectionErrorHandler);
            _clusterView = new RoundRobinClusterView(seedServer);
            _logger = logger;
        }

        public IPooledConnection AcquireConnection(AccessMode mode)
        {
            EnsureDiscovery();
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

        private IPooledConnection AcquireReadConnection()
        {
            while (true)
            {
                Uri uri;
                if (!_clusterView.TryNextReader(out uri))
                {
                    // no server known to clusterView
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
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

        private IPooledConnection AcquireWriteConnection()
        {
            while(true)
            {
                Uri uri;
                if (!_clusterView.TryNextWriter(out uri))
                {
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
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
            _clusterView.Remove(uri);
            _connectionPool.Purge(uri);
        }

        public void EnsureDiscovery()
        {
            lock (_syncLock)
            {
                if (!_clusterView.IsStale())
                {
                    return;
                }

                var newView = NewClusterView();
                _connectionPool.Update(newView.All());
                _clusterView = newView;
            }
        }

        public RoundRobinClusterView NewClusterView()
        {
            while (true)
            {
                Uri uri;
                if (!_clusterView.TryNextRouter(out uri))
                {
                    // no alive server
                    break;
                }

                try
                {
                    IPooledConnection conn;
                    if (_connectionPool.TryAcquire(uri, out conn))
                    {
                        var discoveryManager = new ClusterDiscoveryManager(conn, _logger);
                        discoveryManager.Rediscovery();
                        return new RoundRobinClusterView(discoveryManager.Routers, discoveryManager.Readers,
                            discoveryManager.Writers);
                    }
                }
                catch (SessionExpiredException)
                {
                    // ignored
                    // Already handled by connection pool error handler to remove from load balancer
                }
                catch (InvalidDiscoveryException)
                {
                    _clusterView.Remove(uri);
                }
            }

            // TODO also try each detached routers
            // We retied and tried our best however there is just no cluster.
            // This is the ultimate place we will inform the user that you need to re-create a driver
            throw new ServerUnavailableException(
                "Failed to connect to any routing server. " +
                "Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }

        private Exception OnConnectionError(Exception e, Uri uri)
        {
            Forget(uri);
            return new SessionExpiredException($"Server at {uri} is no longer available", e);
        }

        private Neo4jException OnNeo4jError(Neo4jException error, Uri uri)
        {
            if (error.Code.Equals("Neo.ClientError.Cluster.NotALeader"))
            {
                // The lead is no longer a leader, a.k.a. the write server no longer accepts writes
                // However the server is still available for possible reads.
                // Therefore we just remove it from ClusterView but keep it in connection pool.
                _clusterView.Remove(uri);
                return new SessionExpiredException($"Server at {uri} no longer accepts writes");
            }
            else if (error.Code.Equals("Neo.ClientError.General.ForbiddenOnReadOnlyDatabase"))
            {
                // The user was trying to run a write in a read session
                // So inform the user and let him try with a proper session mode
                return new ClientException("Write queries cannot be performed in READ access mode.");
            }
            return error;
        }

        private ClusterPooledConnectionErrorHandler CreateClusterPooledConnectionErrorHandler(Uri uri)
        {
            return new ClusterPooledConnectionErrorHandler(x => OnConnectionError(x, uri), x => OnNeo4jError(x, uri));
        }

        private class ClusterPooledConnectionErrorHandler : IConnectionErrorHandler
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

            public Neo4jException OnNeo4jError(Neo4jException e)
            {
                return _onNeo4jErrorFunc.Invoke(e);
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            _clusterView = null;

            if (_connectionPool != null)
            {
                _connectionPool.Dispose();
                _connectionPool = null;
            }
            _logger = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}