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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools = new ConcurrentDictionary<Uri, IConnectionPool>();
        private readonly IAuthToken _authToken;
        private readonly EncryptionManager _encryptionManager;
        private readonly ConnectionPoolSettings _poolSettings;
        private readonly Func<Uri, IConnectionErrorHandler> _clusterErrorHandlerCreator;

        // for test only
        private readonly IConnectionPool _fakeConnectionPool;

        private volatile bool _disposeCalled;

        public ClusterConnectionPool(
            Uri seedServer,
            IAuthToken authToken,
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings,
            ILogger logger,
            Func<Uri, IConnectionErrorHandler> clusterErrorHandlerCreator)
            : base(logger)
        {
            _authToken = authToken;
            _encryptionManager = encryptionManager;
            _poolSettings = poolSettings;
            _clusterErrorHandlerCreator = clusterErrorHandlerCreator;
            if (seedServer != null)
            {
                Add(seedServer);
            }
        }

        internal ClusterConnectionPool(IConnectionPool connectionPool,
            ConcurrentDictionary<Uri, IConnectionPool> clusterPool=null,
            ConnectionPoolSettings poolSettings=null,
            ILogger logger=null) :
            this(null, null, null, poolSettings, logger, null)
        {
            _fakeConnectionPool = connectionPool;
            _pools = clusterPool;
        }

        private IConnectionPool CreateNewConnectionPool(Uri uri)
        {
            return _fakeConnectionPool ?? new ConnectionPool(uri, _authToken, _encryptionManager, _poolSettings, Logger, _clusterErrorHandlerCreator.Invoke(uri));
        }

        public bool TryAcquire(Uri uri, out IPooledConnection conn)
        {
            IConnectionPool pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                conn = null;
                return false;
            }

            conn = pool.Acquire();
            return true;
        }

        // This is the ultimate method to add a pool
        private void Add(Uri uri)
        {
            _pools.GetOrAdd(uri, CreateNewConnectionPool);
            if (_disposeCalled)
            {
                // Anything added after dispose should be directly cleaned.
                Purge(uri);
                throw new InvalidOperationException($"Failed to create connections with server {uri} as the driver has already started to dispose.");
            }
        }

        public void Update(IEnumerable<Uri> servers)
        {
            foreach (var uri in _pools.Keys)
            {
                if (!servers.Contains(uri))
                {
                    Purge(uri);
                }
            }
            foreach (var uri in servers)
            {
                Add(uri);
            }
        }

        public void Purge(Uri uri)
        {
            IConnectionPool toRemvoe;
            var removed = _pools.TryRemove(uri, out toRemvoe);
            if (removed)
            {
                toRemvoe.Dispose();
            }
        }

        private void Clear()
        {
            var uris = _pools.Keys;
            foreach (var uri in uris)
            {
                Purge(uri);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            _disposeCalled = true;
            Clear();
        }
    }
}
