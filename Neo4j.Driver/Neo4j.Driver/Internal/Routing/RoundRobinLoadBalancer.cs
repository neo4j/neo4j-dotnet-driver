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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoundRobinLoadBalancer : ILoadBalancer
    {
        private RoundRobinClusterView _clusterView;
        private readonly IClusterConnectionPool _connectionPool;

        public RoundRobinLoadBalancer(
            Uri seedServer,
            IAuthToken authToken,
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            _connectionPool = new ClusterConnectionPool(authToken, encryptionManager, poolSettings, logger);
            _connectionPool.Add(seedServer);
            _clusterView = new RoundRobinClusterView(seedServer);
        }

        public IPooledConnection AcquireConnection(AccessMode mode)
        {
            Discovery();
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
                catch (ConnectionFailureException)
                {
                    Forget(uri);
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
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }
            throw new SessionExpiredException("Failed to connect to any write server.");
        }

        public void Forget(Uri uri)
        {
            _clusterView.Remove(uri);
            _connectionPool.Purge(uri);
        }

        // TODO: Should sync on this method
        public void Discovery()
        {
            if (!_clusterView.IsStale())
            {
                return;
            }

            var oldServers = _clusterView.All();
            var newView = NewClusterView();
            var newServers = newView.All();

            oldServers.ExceptWith(newServers);
            foreach (var server in oldServers)
            {
                _connectionPool.Purge(server);
            }
            foreach (var server in newServers)
            {
                _connectionPool.Add(server);
            }
            
            _clusterView = newView;
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
                        var discoveryManager = new ClusterDiscoveryManager(conn);
                        discoveryManager.Rediscovery();
                        return new RoundRobinClusterView(discoveryManager.Routers, discoveryManager.Readers, discoveryManager.Writers);
                    }
                }
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }

            // TODO also try each detached routers
            throw new SessionExpiredException(
                "Failed to connect to any routing server. " +
                "Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }
    }
}