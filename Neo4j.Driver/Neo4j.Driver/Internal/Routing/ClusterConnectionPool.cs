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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionProvider> _pools = new ConcurrentDictionary<Uri, IConnectionProvider>();
        private readonly ConnectionSettings _connectionSettings;
        private readonly ConnectionPoolSettings _poolSettings;

        // for test only
        private readonly IConnectionProvider _fakePool;

        private volatile bool _disposeCalled;

        public ClusterConnectionPool(
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings,
            IEnumerable<Uri> initUris, ILogger logger
            )
            : base(logger)
        {
            _connectionSettings = connectionSettings;
            _poolSettings = poolSettings;
            Add(initUris);
        }

        internal ClusterConnectionPool(
            IConnectionProvider connectionPool,
            ConcurrentDictionary<Uri, IConnectionProvider> clusterPool=null,
            ConnectionSettings connSettings=null,
            ConnectionPoolSettings poolSettings=null,
            ILogger logger=null
            ) :
            this(connSettings, poolSettings, Enumerable.Empty<Uri>(), logger)
        {
            _fakePool = connectionPool;
            _pools = clusterPool;
        }

        private IConnectionProvider CreateNewConnectionPool(Uri uri)
        {
            return _fakePool ?? new ConnectionPool(uri, _connectionSettings, _poolSettings, Logger);
        }

        public IConnection Acquire(Uri uri)
        {
            IConnectionProvider pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                return null;
            }

            AccessMode ignored = AccessMode.Write;
            return pool.Acquire(ignored);
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
            IConnectionProvider toRemvoe;
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
