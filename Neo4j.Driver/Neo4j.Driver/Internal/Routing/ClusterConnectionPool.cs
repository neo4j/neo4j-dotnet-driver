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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : LoggerBase, IClusterConnectionPool
    {
        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools =
            new ConcurrentDictionary<Uri, IConnectionPool>();

        private readonly ConnectionSettings _connectionSettings;
        private readonly ConnectionPoolSettings _poolSettings;
        private readonly BufferSettings _bufferSettings;

        // for test only
        private readonly IConnectionPool _fakePool;

        private int _closedMarker = 0;

        public ClusterConnectionPool(
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings,
            BufferSettings bufferSettings,
            IEnumerable<Uri> initUris, ILogger logger
        )
            : base(logger)
        {
            _connectionSettings = connectionSettings;
            _poolSettings = poolSettings;
            _bufferSettings = bufferSettings;
            Add(initUris);
        }

        internal ClusterConnectionPool(
            IConnectionPool connectionPool,
            ConcurrentDictionary<Uri, IConnectionPool> clusterPool = null,
            ConnectionSettings connSettings = null,
            ConnectionPoolSettings poolSettings = null,
            BufferSettings bufferSettings = null,
            ILogger logger = null
        ) :
            this(connSettings, poolSettings, bufferSettings, Enumerable.Empty<Uri>(), logger)
        {
            _fakePool = connectionPool;
            _pools = clusterPool;
        }

        private IConnectionPool CreateNewConnectionPool(Uri uri)
        {
            return _fakePool ?? new ConnectionPool(uri, _connectionSettings, _poolSettings, _bufferSettings, Logger);
        }

        public IConnection Acquire(Uri uri)
        {
            IConnectionPool pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                return null;
            }

            AccessMode ignored = AccessMode.Write;
            return pool.Acquire(ignored);
        }

        public Task<IConnection> AcquireAsync(Uri uri)
        {
            IConnectionPool pool;
            if (!_pools.TryGetValue(uri, out pool))
            {
                return Task.FromResult((IConnection)null);
            }

            AccessMode ignored = AccessMode.Write;
            return pool.AcquireAsync(ignored);
        }

        // This is the ultimate method to add a pool
        private void Add(Uri uri)
        {
            _pools.GetOrAdd(uri, CreateNewConnectionPool);
            if (_closedMarker > 0)
            {
                // Anything added after dispose should be directly cleaned.
                Clear();
                throw new ObjectDisposedException(GetType().Name,
                    $"Failed to create connections with server {uri} as the driver has already started to dispose.");
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
            var removed = _pools.TryRemove(uri, out var toRemove);
            if (removed)
            {
                toRemove.Close();
            }
        }

        public Task PurgeAsync(Uri uri)
        {
            var removed = _pools.TryRemove(uri, out var toRemove);
            if (removed)
            {
                return toRemove.CloseAsync();
            }

            return TaskExtensions.GetCompletedTask();
        }

        public int NumberOfInUseConnections(Uri uri)
        {
            IConnectionPool pool;
            if (_pools.TryGetValue(uri, out pool))
            {
                return pool.NumberOfInUseConnections;
            }
            return 0;
        }

        private void Clear()
        {
            var uris = _pools.Keys;
            foreach (var uri in uris)
            {
                Purge(uri);
            }
        }

        private Task ClearAsync()
        {
            var clearTasks = new List<Task>();

            var uris = _pools.Keys;
            foreach (var uri in uris)
            {
                clearTasks.Add(PurgeAsync(uri));
            }

            return Task.WhenAll(clearTasks);
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                Clear();
            }
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return ClearAsync();
            }

            return TaskExtensions.GetCompletedTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (_closedMarker > 0)
                return;

            if (disposing)
            {
                Close();
            }

            base.Dispose(disposing);
        }

        public override string ToString()
        {
            return _pools.ValueToString();
        }
    }
}