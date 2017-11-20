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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoutingTableManager : IRoutingTableManager
    {
        private readonly ILogger _logger;

        private readonly Uri _seedUri;
        private readonly IDictionary<string, string> _routingContext;

        private IRoutingTable _routingTable;

        public IRoutingTable RoutingTable
        {
            get => _routingTable;
            set => _routingTable = value;
        }

        private readonly IClusterConnectionPoolManager _poolManager;

        private readonly object _syncLock = new object();
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private bool _isReadingInAbsenceOfWriter = false;

        public bool IsReadingInAbsenceOfWriter
        {
            get => _isReadingInAbsenceOfWriter;
            set => _isReadingInAbsenceOfWriter = value;
        }

        public RoutingTableManager(
            RoutingSettings routingSettings,
            IClusterConnectionPoolManager poolManager,
            ISet<Uri> initUris,
            ILogger logger) :
            this(new RoutingTable(initUris),
                routingSettings, poolManager, logger)
        {
        }

        public RoutingTableManager(
            IRoutingTable routingTable,
            RoutingSettings routingSettings,
            IClusterConnectionPoolManager poolManager,
            ILogger logger)
        {
            _routingTable = routingTable;
            _routingContext = routingSettings.RoutingContext;
            _seedUri = routingSettings.InitialServerUri;

            _poolManager = poolManager;
            _logger = logger;
        }

        public void EnsureRoutingTableForMode(AccessMode mode)
        {
            // a quick return path for most happy cases
            if (!IsRoutingTableStale(_routingTable, mode))
            {
                return;
            }

            lock (_syncLock)
            {
                // once we grab the lock, we test again to avoid update it multiple times 
                if (!IsRoutingTableStale(_routingTable, mode))
                {
                    return;
                }

                var routingTable = UpdateRoutingTableWithInitialUriFallback(new HashSet<Uri> { _seedUri });
                Update(routingTable);

            }
        }

        public async Task EnsureRoutingTableForModeAsync(AccessMode mode)
        {
            // a quick return path for most happy cases
            if (!IsRoutingTableStale(_routingTable, mode))
            {
                return;
            }

            // now lock
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // test against to avoid update it multipule times
                if (!IsRoutingTableStale(_routingTable, mode))
                {
                    return;
                }

                var routingTable = await UpdateRoutingTableWithInitialUriFallbackAsync(new HashSet<Uri> { _seedUri }).ConfigureAwait(false);
                Update(routingTable);
            }
            finally
            {
                // no matter whether we succes to update or not, we release the lock
                _semaphore.Release();
            }
        }

        private void Update(IRoutingTable newTable)
        {
            var added = newTable.All();
            added.ExceptWith(_routingTable.All());
            var removed = _routingTable.All();
            removed.ExceptWith(newTable.All());

            _poolManager.UpdateConnectionPool(added, removed);
            _routingTable = newTable;

            _logger?.Info($"Updated routingTable to be {_routingTable}");
        }

        private bool IsRoutingTableStale(IRoutingTable routingTable, AccessMode mode = AccessMode.Read)
        {
            switch (mode)
            {
                case AccessMode.Read:
                    if (routingTable.IsStale(AccessMode.Read))
                    {
                        return true;
                    }
                    _isReadingInAbsenceOfWriter = routingTable.IsStale(AccessMode.Write);
                    return false;
                case AccessMode.Write:
                    return routingTable.IsStale(AccessMode.Write);
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }
        }

        private void PrependRouters(ISet<Uri> uris)
        {
            _routingTable.PrependRouters(uris);
            _poolManager.AddConnectionPool(uris);
        }

        internal IRoutingTable UpdateRoutingTableWithInitialUriFallback(ISet<Uri> initialUriSet,
            Func<ISet<Uri>, IRoutingTable> updateRoutingTableFunc = null)
        {
            updateRoutingTableFunc = updateRoutingTableFunc ?? (u => UpdateRoutingTable(u));

            var hasPrependedInitialRouters = false;
            if (_isReadingInAbsenceOfWriter)
            {
                PrependRouters(initialUriSet);
                hasPrependedInitialRouters = true;
            }

            var triedUris = new HashSet<Uri>();
            var routingTable = updateRoutingTableFunc(triedUris);
            if (routingTable != null)
            {
                return routingTable;
            }

            if (!hasPrependedInitialRouters)
            {
                var uris = initialUriSet;
                uris.ExceptWith(triedUris);
                if (uris.Count != 0)
                {
                    PrependRouters(uris);
                    routingTable = updateRoutingTableFunc(null);
                    if (routingTable != null)
                    {
                        return routingTable;
                    }
                }
            }
            // We retied and tried our best however there is just no cluster.
            // This is the ultimate place we will inform the user that you need to re-create a driver
            throw new ServiceUnavailableException(
                "Failed to connect to any routing server. " +
                "Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }

        internal async Task<IRoutingTable> UpdateRoutingTableWithInitialUriFallbackAsync(ISet<Uri> initialUriSet,
            Func<ISet<Uri>, Task<IRoutingTable>> updateRoutingTableFunc = null)
        {
            updateRoutingTableFunc = updateRoutingTableFunc ?? (u => UpdateRoutingTableAsync(u));

            var hasPrependedInitialRouters = false;
            if (_isReadingInAbsenceOfWriter)
            {
                PrependRouters(initialUriSet);
                hasPrependedInitialRouters = true;
            }

            var triedUris = new HashSet<Uri>();
            var routingTable = await updateRoutingTableFunc(triedUris).ConfigureAwait(false);
            if (routingTable != null)
            {
                return routingTable;
            }

            if (!hasPrependedInitialRouters)
            {
                var uris = initialUriSet;
                uris.ExceptWith(triedUris);
                if (uris.Count != 0)
                {
                    PrependRouters(uris);
                    routingTable = await updateRoutingTableFunc(null).ConfigureAwait(false);
                    if (routingTable != null)
                    {
                        return routingTable;
                    }
                }
            }
            // We retied and tried our best however there is just no cluster.
            // This is the ultimate place we will inform the user that you need to re-create a driver
            throw new ServiceUnavailableException(
                "Failed to connect to any routing server. " +
                "Please make sure that the cluster is up and can be accessed by the driver and retry.");
        }

        public IRoutingTable UpdateRoutingTable(ISet<Uri> triedUris = null, Func<IConnection, IRoutingTable> rediscoveryFunc = null)
        {
            rediscoveryFunc = rediscoveryFunc ?? Rediscovery;

            var knownRouters = _routingTable.Routers;
            foreach (var router in knownRouters)
            {
                triedUris?.Add(router);
                IConnection conn = _poolManager.CreateClusterConnection(router);
                if (conn == null)
                {
                    _routingTable.Remove(router);
                }
                else
                {
                    try
                    {
                        var roundRobinRoutingTable = rediscoveryFunc(conn);
                        if (!IsRoutingTableStale(roundRobinRoutingTable))
                        {
                            return roundRobinRoutingTable;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.Info(
                            $"Failed to update routing table with server uri={router} due to error {e.Message}");
                        if (e is SessionExpiredException)
                        {
                            // ignored
                            // Already handled by clusterConn.OnConnectionError to remove from load balancer
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            return null;
        }

        public async Task<IRoutingTable> UpdateRoutingTableAsync(ISet<Uri> triedUris = null, Func<IConnection, Task<IRoutingTable>> rediscoveryFunc = null)
        {
            rediscoveryFunc = rediscoveryFunc ?? RediscoveryAsync;

            var knownRouters = _routingTable.Routers;
            foreach (var router in knownRouters)
            {
                triedUris?.Add(router);
                IConnection conn = await _poolManager.CreateClusterConnectionAsync(router).ConfigureAwait(false);
                if (conn == null)
                {
                    _routingTable.Remove(router);
                }
                else
                {
                    try
                    {
                        var roundRobinRoutingTable = await rediscoveryFunc(conn).ConfigureAwait(false);
                        if (!IsRoutingTableStale(roundRobinRoutingTable))
                        {
                            return roundRobinRoutingTable;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.Info(
                            $"Failed to update routing table with server uri={router} due to error {e.Message}");
                        if (e is SessionExpiredException)
                        {
                            // ignored
                            // Already handled by clusterConn.OnConnectionError to remove from load balancer
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            return null;
        }

        private IRoutingTable Rediscovery(IConnection conn)
        {
            var discoveryManager = new ClusterDiscoveryManager(conn, _routingContext, _logger);
            discoveryManager.Rediscovery();
            return new RoutingTable(discoveryManager.Routers, discoveryManager.Readers,
                discoveryManager.Writers, discoveryManager.ExpireAfterSeconds);
        }

        private async Task<IRoutingTable> RediscoveryAsync(IConnection conn)
        {
            var discoveryManager = new ClusterDiscoveryManager(conn, _routingContext, _logger);
            await discoveryManager.RediscoveryAsync().ConfigureAwait(false);
            return new RoutingTable(discoveryManager.Routers, discoveryManager.Readers,
                discoveryManager.Writers, discoveryManager.ExpireAfterSeconds);
        }

        public void Clear()
        {
            _routingTable.Clear();
        }
    }
}
