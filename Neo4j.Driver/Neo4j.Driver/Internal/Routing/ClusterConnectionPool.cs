// Copyright (c) 2002-2018 "Neo Technology,"
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
        private readonly IConnectionPoolFactory _poolFactory;

        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools =
            new ConcurrentDictionary<Uri, IConnectionPool>();

        private int _closedMarker = 0;

        public ClusterConnectionPool(
            IEnumerable<Uri> initUris,
            IPooledConnectionFactory connectionFactory,
            ConnectionPoolSettings poolSettings,
            ILogger logger
        ) : this(initUris, new ConnectionPoolFactory(connectionFactory, poolSettings, logger), logger)
        {
        }

        // test only
        internal ClusterConnectionPool(
            IConnectionPoolFactory poolFactory,
            ConcurrentDictionary<Uri, IConnectionPool> clusterPool,
            ILogger logger = null
        ) :
            this(Enumerable.Empty<Uri>(), poolFactory, logger)
        {
            _pools = clusterPool;
        }


        private ClusterConnectionPool(IEnumerable<Uri> initUris,
            IConnectionPoolFactory poolFactory, ILogger logger)
            : base(logger)
        {
            _poolFactory = poolFactory;
            Add(initUris);
        }

        private bool IsClosed => _closedMarker > 0;

        public IConnection Acquire(Uri uri)
        {
            if (!_pools.TryGetValue(uri, out var pool))
            {
                return null;
            }

            AccessMode ignored = AccessMode.Write;
            return pool.Acquire(ignored);
        }

        public Task<IConnection> AcquireAsync(Uri uri)
        {
            if (!_pools.TryGetValue(uri, out var pool))
            {
                return Task.FromResult((IConnection)null);
            }

            AccessMode ignored = AccessMode.Write;
            return pool.AcquireAsync(ignored);
        }

        public void Add(IEnumerable<Uri> servers)
        {
            foreach (var uri in servers)
            {
                _pools.AddOrUpdate(uri, _poolFactory.Create, ActivateConnectionPool);
            }
            if (IsClosed)
            {
                // Anything added after dispose should be directly cleaned.
                Clear();
                throw new ObjectDisposedException(GetType().Name,
                    $"Failed to create connections with servers {servers.ToContentString()} as the driver has already started to dispose.");
            }
        }

        public async Task AddAsync(IEnumerable<Uri> servers)
        {
            foreach (var uri in servers)
            {
                _pools.AddOrUpdate(uri, _poolFactory.Create, ActivateConnectionPool);
            }
            if (IsClosed)
            {
                // Anything added after dispose should be directly cleaned.
                await ClearAsync().ConfigureAwait(false);
                throw new ObjectDisposedException(GetType().Name,
                    $"Failed to create connections with servers {servers.ToContentString()} as the driver has already started to dispose.");
            }
        }

        public void Update(IEnumerable<Uri> added, IEnumerable<Uri> removed)
        {
            Add(added);
            foreach (var uri in removed)
            {
                if (_pools.TryGetValue(uri, out var pool))
                {
                    pool.Deactivate();
                    if (pool.NumberOfInUseConnections == 0)
                    {
                        Purge(uri);
                    }
                }
            }
        }

        public async Task UpdateAsync(IEnumerable<Uri> added, IEnumerable<Uri> removed)
        {
            await AddAsync(added).ConfigureAwait(false);
            foreach (var uri in removed)
            {
                if (_pools.TryGetValue(uri, out var pool))
                {
                    await pool.DeactivateAsync().ConfigureAwait(false);
                    if (pool.NumberOfInUseConnections == 0)
                    {
                        await PurgeAsync(uri).ConfigureAwait(false);
                    }
                }
            }
        }

        public void Deactivate(Uri uri)
        {
            if (_pools.TryGetValue(uri, out var pool))
            {
                pool.Deactivate();
            }
        }

        public Task DeactivateAsync(Uri uri)
        {
            if (_pools.TryGetValue(uri, out var pool))
            {
                return pool.DeactivateAsync();
            }
            return TaskExtensions.GetCompletedTask();
        }

        public int NumberOfInUseConnections(Uri uri)
        {
            if (_pools.TryGetValue(uri, out var pool))
            {
                return pool.NumberOfInUseConnections;
            }
            return 0;
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

        private void Purge(Uri uri)
        {
            var removed = _pools.TryRemove(uri, out var toRemove);
            if (removed)
            {
                toRemove.Close();
            }
        }

        private Task PurgeAsync(Uri uri)
        {
            var removed = _pools.TryRemove(uri, out var toRemove);
            if (removed)
            {
                return toRemove.CloseAsync();
            }

            return TaskExtensions.GetCompletedTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsClosed)
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

        private IConnectionPool ActivateConnectionPool(Uri uri, IConnectionPool pool)
        {
            pool.Activate();
            return pool;
        }
    }
}
