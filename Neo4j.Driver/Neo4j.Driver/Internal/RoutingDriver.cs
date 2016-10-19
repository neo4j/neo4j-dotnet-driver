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

namespace Neo4j.Driver.Internal
{
    internal class RoutingDriver : IDriver
    {
        private IClusterConnectionPool _connectionPool;
        private ILogger _logger;
        private ClusterView _clusterView;

        internal RoutingDriver(
            Uri seedServer, 
            IAuthToken authToken, 
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings, 
            ILogger logger)
        {
            Uri = seedServer;
            _logger = logger;
            _connectionPool = new ClusterConnectionPool(authToken, encryptionManager, poolSettings, logger);
            _clusterView = new ClusterView(seedServer);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Uri Uri { get; }
        public ISession Session()
        {
            return Session(AccessMode.Write);
        }

        public ISession Session(AccessMode mode)
        {
            IPooledConnection connection = AcquireConnection(mode);

            throw new NotImplementedException();
            //return new RoutingSession(connection, mode, )
        }

        private IPooledConnection AcquireConnection(AccessMode mode)
        {
            CheckServer();
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
            for (var i = 0; i < _clusterView.NumberOfReaders; i++)
            {
                Uri uri;
                if (_clusterView.TryNextReader(out uri))
                {
                    break; // while we are reading from the view, all servers get removed from the view
                }

                try
                {
                    return _connectionPool.Acquire(uri);
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
            for (var i = 0; i < _clusterView.NumberOfWriters; i++)
            {
                Uri uri;
                if (_clusterView.TryNextWriter(out uri))
                {
                    break;
                }

                try
                {
                    return _connectionPool.Acquire(uri);
                }
                catch (ConnectionFailureException)
                {
                    Forget(uri);
                }
            }
            throw new SessionExpiredException("Failed to connect to any write server.");
        }

        private void Forget(Uri uri)
        {
            _connectionPool.Purge(uri);
            _clusterView.Remove(uri);
        }

        // Should sync on this method
        private void CheckServer()
        {
            throw new NotImplementedException();
//            if (!_clusterView.IsStale())
//            {
//                return;
//            }
//            var oldCluster = _clusterView.All();
//            var newView = NewClusterView();
//            var newCluster = newView.All();
//
//            oldCluster.ExceptWith(newCluster);
//            foreach (var server in oldCluster)
//            {
//                _connectionPool.Purge(server);
//            }
//            _clusterView = newView;
        }

        private ClusterView NewClusterView()
        {
            throw new NotImplementedException();
        }
    }
}