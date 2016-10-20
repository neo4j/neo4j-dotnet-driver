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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools = new ConcurrentDictionary<Uri, IConnectionPool>();
        private readonly IAuthToken _authToken;
        private readonly EncryptionManager _encryptionManager;
        private readonly ConnectionPoolSettings _poolSettings;

        // for test only
        private readonly IConnectionPool _fakeConnectionPool;

        private volatile bool _disposeCalled;

        public ClusterConnectionPool(IAuthToken authToken, EncryptionManager encryptionManager, ConnectionPoolSettings poolSettings, ILogger logger)
            : base(logger)
        {
            _authToken = authToken;
            _encryptionManager = encryptionManager;
            _poolSettings = poolSettings;
        }

        internal ClusterConnectionPool(IConnectionPool connectionPool,
            ConcurrentDictionary<Uri, IConnectionPool> pool=null,
            ConnectionPoolSettings poolSettings=null,
            ILogger logger=null) :
            this(null, encryptionManager: null, poolSettings: poolSettings, logger: logger)
        {
            _fakeConnectionPool = connectionPool;
            _pools = pool;
        }

        private IConnectionPool CreateNewConnectionPool(Uri uri)
        {
            return _fakeConnectionPool ?? new ConnectionPool(uri, _authToken, _encryptionManager, _poolSettings, Logger);
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

        public bool HasAddress(Uri uri)
        {
            return _pools.ContainsKey(uri);
        }

        // This is the only place to add a pool
        public void Add(Uri uri)
        {
            _pools.GetOrAdd(uri, CreateNewConnectionPool);
            if (_disposeCalled)
            {
                // Anything added after dispose should be directly cleaned.
                Purge(uri);
                throw new InvalidOperationException($"Failed to create connections with server {uri} as the driver has already started to dispose.");
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

        public void Clear()
        {
            var uris = _pools.Keys;
            foreach (var uri in uris)
            {
                Purge(uri);
            }
        }

        public void Release(Uri uri, Guid id)
        {
            IConnectionPool pool;
            var found = _pools.TryGetValue(uri, out pool);
            if (found)
            {
                pool.Release(id);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            _disposeCalled = true;
            Clear();
        }
    }
}
