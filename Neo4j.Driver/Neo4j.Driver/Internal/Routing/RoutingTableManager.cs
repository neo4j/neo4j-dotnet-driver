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

        private bool _isReadingInAbsenceOfWriter = false;
        public bool IsReadingInAbsenceOfWriter
        {
            get => _isReadingInAbsenceOfWriter;
            set => _isReadingInAbsenceOfWriter = value;
        }

        public RoutingTableManager(
            RoutingSettings routingSettings,
            IClusterConnectionPoolManager poolManager,
            Uri seedUri,
            ISet<Uri> initUris,
            ILogger logger) :
            this(new RoundRobinRoutingTable(initUris),
                routingSettings, poolManager, seedUri, logger)
        {
        }

        public RoutingTableManager(
            IRoutingTable routingTable, 
            RoutingSettings routingSettings,
            IClusterConnectionPoolManager poolManager,
            Uri seedUri,
            ILogger logger)
        {
            _routingTable = routingTable;
            _routingContext = routingSettings.RoutingContext;
            _poolManager = poolManager;
            _seedUri = seedUri;

            _logger = logger;
        }

        public bool TryAcquireConnection(AccessMode mode, out Uri uri)
        {
            return _routingTable.TryNext(mode, out uri);
        }

        public void EnsureRoutingTableForMode(AccessMode mode)
        {
            lock (_syncLock)
            {
                if (!IsRoutingTableStale(_routingTable, mode))
                {
                    return;
                }

                var routingTable = UpdateRoutingTableWithInitialUriFallback(_seedUri.Resolve());
                _poolManager.UpdateConnectionPool(routingTable.All());
                _routingTable = routingTable;
                _logger?.Info($"Updated routingTable to be {_routingTable}");
            }
        }

        public bool IsRoutingTableStale(IRoutingTable routingTable, AccessMode mode = AccessMode.Read)
        {
            lock (_syncLock)
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
        }

        public void PrependRouters(ISet<Uri> uris)
        {
            lock (_syncLock)
            {
                _routingTable.PrependRouters(uris);
                _poolManager.AddConnectionPool(uris);
            }
        }

        public IRoutingTable UpdateRoutingTableWithInitialUriFallback(ISet<Uri> initialUriSet,
            Func<ISet<Uri>, IRoutingTable> updateRoutingTableFunc = null)
        {
            lock (_syncLock)
            {
                updateRoutingTableFunc = updateRoutingTableFunc ?? (u => UpdateRoutingTable(null, u));

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
        }

        public IRoutingTable UpdateRoutingTable(Func<IConnection, IRoutingTable> rediscoveryFunc = null,
            ISet<Uri> triedUris = null)
        {
            lock (_syncLock)
            {
                rediscoveryFunc = rediscoveryFunc ?? Rediscovery;
                while (true)
                {
                    Uri uri;
                    if (!_routingTable.TryNextRouter(out uri))
                    {
                        // no alive server
                        return null;
                    }
                    triedUris?.Add(uri);
                    IConnection conn = _poolManager.CreateClusterConnection(uri);
                    if (conn == null)
                    {
                        _routingTable.Remove(uri);
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
                                $"Failed to update routing table with server uri={uri} due to error {e.Message}");
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
            }
        }

        public IRoutingTable Rediscovery(IConnection conn)
        {
            lock (_syncLock)
            {
                var discoveryManager = new ClusterDiscoveryManager(conn, _routingContext, _logger);
                discoveryManager.Rediscovery();
                return new RoundRobinRoutingTable(discoveryManager.Routers, discoveryManager.Readers,
                    discoveryManager.Writers, discoveryManager.ExpireAfterSeconds);
            }
        }

        public void Clear()
        {
            _routingTable.Clear();
        }
    }
}
