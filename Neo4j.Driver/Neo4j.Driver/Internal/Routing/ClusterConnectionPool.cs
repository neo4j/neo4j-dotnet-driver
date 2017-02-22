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
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools = new ConcurrentDictionary<Uri, IConnectionPool>();
        private readonly ConnectionSettings _connectionSettings;
        private readonly ConnectionPoolSettings _poolSettings;
        private readonly Action<Uri, Exception> _onErrorAction;

        // for test only
        private readonly IConnectionPool _fakePool;

        private volatile bool _disposeCalled;

        public ClusterConnectionPool(
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings,
            ILogger logger,
            Action<Uri, Exception> onErrorAction=null)
            : base(logger)
        {
            _connectionSettings = connectionSettings;
            _poolSettings = poolSettings;
            _onErrorAction = onErrorAction ?? ((uri, e)=> { throw e; });

        }

        internal ClusterConnectionPool(
            IConnectionPool connectionPool,
            ConcurrentDictionary<Uri, IConnectionPool> clusterPool=null,
            ConnectionSettings connSettings=null,
            ConnectionPoolSettings poolSettings=null,
            ILogger logger=null,
            Action<Uri, Exception> onErrorAction = null
            ) :
            this(connSettings, poolSettings, logger, onErrorAction)
        {
            _fakePool = connectionPool;
            _pools = clusterPool;
        }

        private IConnectionPool CreateNewConnectionPool(Uri uri)
        {
            return _fakePool ?? new ConnectionPool(uri, _connectionSettings, _poolSettings, Logger);
        }

        public bool TryAcquire(Uri uri, out IClusterConnection conn)
        {
            IConnectionPool pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                conn = null;
                return false;
            }

            conn = CreateNewClusterConnection(pool, uri);
            return true;
        }

        private IClusterConnection CreateNewClusterConnection(IConnectionPool pool, Uri uri)
        {
            return new ClusterConnection(()=>pool.Acquire(), e => _onErrorAction.Invoke(uri, e));
        }

        // This is the ultimate method to add a pool
        private void Add(Uri uri)
        {
            _pools.GetOrAdd(uri, CreateNewConnectionPool);
            if (_disposeCalled)
            {
                // Anything added after dispose should be directly cleaned.
                Clear();
                throw new ObjectDisposedException(GetType().Name, $"Failed to create connections with server {uri} as the driver has already started to dispose.");
            }
        }

        public void Add(IEnumerable<Uri> servers)
        {
            foreach (var uri in servers)
            {
                Add(uri);
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

        public override string ToString()
        {
            return _pools.ValueToString();
        }
    }
}
