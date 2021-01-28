﻿// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterConnectionPool : IClusterConnectionPool
    {
        private readonly IConnectionPoolFactory _poolFactory;

        private readonly ConcurrentDictionary<Uri, IConnectionPool> _pools =
            new ConcurrentDictionary<Uri, IConnectionPool>();

        private int _closedMarker = 0;

        private readonly IDriverLogger _logger;

        public ClusterConnectionPool(
            IEnumerable<Uri> initUris,
            IPooledConnectionFactory connectionFactory,
            ConnectionPoolSettings poolSettings,
            IDriverLogger logger
        ) : this(initUris, new ConnectionPoolFactory(connectionFactory, poolSettings, logger), logger)
        {
        }

        // test only
        internal ClusterConnectionPool(
            IConnectionPoolFactory poolFactory,
            ConcurrentDictionary<Uri, IConnectionPool> clusterPool,
            IDriverLogger logger = null
        ) :
            this(Enumerable.Empty<Uri>(), poolFactory, logger)
        {
            _pools = clusterPool;
        }


        private ClusterConnectionPool(IEnumerable<Uri> initUris,
            IConnectionPoolFactory poolFactory, IDriverLogger logger)
        {
            _logger = logger;
            _poolFactory = poolFactory;
            Add(initUris);
        }

        private bool IsClosed => _closedMarker > 0;

        public IConnection Acquire(Uri uri, AccessMode mode = AccessMode.Write)
        {
            if (!_pools.TryGetValue(uri, out var pool))
            {
                return null;
            }

            return pool.Acquire(mode);
        }

        public Task<IConnection> AcquireAsync(Uri uri, AccessMode mode = AccessMode.Write)
        {
            if (!_pools.TryGetValue(uri, out var pool))
            {
                return Task.FromResult((IConnection) null);
            }

            return pool.AcquireAsync(mode);
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

            return TaskHelper.GetCompletedTask();
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

            return TaskHelper.GetCompletedTask();
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

            return TaskHelper.GetCompletedTask();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsClosed)
                return;

            if (disposing)
            {
                Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return _pools.ToContentString();
        }

        private static IConnectionPool ActivateConnectionPool(Uri uri, IConnectionPool pool)
        {
            pool.Activate();
            return pool;
        }
    }
}