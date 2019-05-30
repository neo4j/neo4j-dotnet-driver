// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Routing
{
    internal class RoutingTableManager : IRoutingTableManager
    {
        private readonly IDriverLogger _logger;
        private readonly IDiscovery _discovery;
        private readonly SyncExecutor _syncExecutor;

        private readonly IClusterConnectionPoolManager _poolManager;
        private readonly IInitialServerAddressProvider _initialServerAddressProvider;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private IRoutingTable _routingTable;

        public bool IsReadingInAbsenceOfWriter { get; set; } = false;

        public RoutingTableManager(
            RoutingSettings routingSettings,
            IClusterConnectionPoolManager poolManager,
            SyncExecutor syncExecutor,
            IDriverLogger logger) :
            this(routingSettings.InitialServerAddressProvider,
                new ClusterDiscovery(syncExecutor, routingSettings.RoutingContext, logger),
                new RoutingTable(Enumerable.Empty<Uri>()), poolManager, syncExecutor, logger)
        {
        }

        public RoutingTableManager(
            IInitialServerAddressProvider initialServerAddressProvider,
            IDiscovery discovery,
            IRoutingTable routingTable,
            IClusterConnectionPoolManager poolManager,
            SyncExecutor syncExecutor,
            IDriverLogger logger)
        {
            _initialServerAddressProvider = initialServerAddressProvider;
            _discovery = discovery;
            _routingTable = routingTable;
            _poolManager = poolManager;
            _syncExecutor = syncExecutor;
            _logger = logger;
        }

        public IRoutingTable RoutingTable => _routingTable;

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
                // test against to avoid update it multiple times
                if (!IsRoutingTableStale(_routingTable, mode))
                {
                    return;
                }

                var routingTable = await UpdateRoutingTableWithInitialUriFallbackAsync().ConfigureAwait(false);
                await UpdateAsync(routingTable).ConfigureAwait(false);
            }
            finally
            {
                // no matter whether we succes to update or not, we release the lock
                _semaphore.Release();
            }
        }

        internal async Task UpdateAsync(IRoutingTable newTable)
        {
            var added = newTable.All();
            added.ExceptWith(_routingTable.All());
            var removed = _routingTable.All();
            removed.ExceptWith(newTable.All());

            await _poolManager.UpdateConnectionPoolAsync(added, removed).ConfigureAwait(false);
            _routingTable = newTable;

            _logger?.Info("Updated routing table to be {0}", _routingTable);
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

                    IsReadingInAbsenceOfWriter = routingTable.IsStale(AccessMode.Write);
                    return false;
                case AccessMode.Write:
                    return routingTable.IsStale(AccessMode.Write);
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }
        }

        private Task PrependRoutersAsync(ISet<Uri> uris)
        {
            _routingTable.PrependRouters(uris);
            return _poolManager.AddConnectionPoolAsync(uris);
        }

        internal async Task<IRoutingTable> UpdateRoutingTableWithInitialUriFallbackAsync(
            Func<ISet<Uri>, Task<IRoutingTable>> updateRoutingTableFunc = null)
        {
            _logger?.Debug("Updating routing table.");

            var hasPrependedInitialRouters = false;
            if (IsReadingInAbsenceOfWriter)
            {
                var uris = _initialServerAddressProvider.Get();
                await PrependRoutersAsync(uris).ConfigureAwait(false);
                hasPrependedInitialRouters = true;
            }

            var triedUris = new HashSet<Uri>();
            var routingTable = await UpdateRoutingTableAsync(triedUris).ConfigureAwait(false);
            if (routingTable != null)
            {
                return routingTable;
            }

            if (!hasPrependedInitialRouters)
            {
                var uris = _initialServerAddressProvider.Get();
                uris.ExceptWith(triedUris);
                if (uris.Count != 0)
                {
                    await PrependRoutersAsync(uris).ConfigureAwait(false);
                    routingTable = await UpdateRoutingTableAsync(null).ConfigureAwait(false);
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

        public async Task<IRoutingTable> UpdateRoutingTableAsync(ISet<Uri> triedUris = null,
            Func<IConnection, Task<IRoutingTable>> rediscoveryFunc = null)
        {
            var knownRouters = _routingTable.Routers;
            foreach (var router in knownRouters)
            {
                triedUris?.Add(router);
                try
                {
                    var conn = await _poolManager.CreateClusterConnectionAsync(router).ConfigureAwait(false);
                    if (conn == null)
                    {
                        _routingTable.Remove(router);
                    }
                    else
                    {
                        var newRoutingTable = await _discovery.DiscoverAsync(conn).ConfigureAwait(false);
                        if (!IsRoutingTableStale(newRoutingTable))
                        {
                            return newRoutingTable;
                        }
                    }
                }
                catch (SecurityException e)
                {
                    _logger?.Error(e,
                        "Failed to update routing table from server '{0}' because of a security exception.", router);
                    throw;
                }
                catch (Exception e)
                {
                    _logger?.Warn(e, "Failed to update routing table from server '{0}'.", router);
                }
            }

            return null;
        }
    }
}