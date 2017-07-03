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
using System.Diagnostics;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Throw.DriverDisposedException;

namespace Neo4j.Driver.Internal.Routing
{
    internal class LoadBalancer : IConnectionProvider, IClusterErrorHandler
    {
        private IRoutingTable _routingTable;
        private readonly IClusterConnectionPool _clusterConnectionPool;
        private readonly IDictionary<string, string> _routingContext;
        private readonly ILogger _logger;
        private readonly object _syncLock = new object();
        private readonly Stopwatch _stopwatch;
        private readonly Uri _seed;

        private volatile bool _disposeCalled = false;
        private bool _isReadingInAbsenceOfWriter = false;

        public LoadBalancer(
            RoutingSettings routingSettings,
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            _clusterConnectionPool = new ClusterConnectionPool(
                connectionSettings, poolSettings, logger);

            _stopwatch = new Stopwatch();
            _routingTable = new RoundRobinRoutingTable(_stopwatch);

            _seed = connectionSettings.InitialServerUri;
            _routingContext = routingSettings.RoutingContext;
            _logger = logger;

            var uris = _seed.Resolve();
            PrependRouters(uris);
        }

        // for test only
        internal LoadBalancer(
            IClusterConnectionPool clusterConnPool,
            IRoutingTable routingTable,
            Uri seed = null)
        {
            _clusterConnectionPool = clusterConnPool;
            _routingTable = routingTable;
            _seed = seed;
        }

        public IConnection Acquire(AccessMode mode)
        {
            if (_disposeCalled)
            {
                ThrowObjectDisposedException();
            }

            IConnection conn = null;
            switch (mode)
            {
                case AccessMode.Read:
                    conn = AcquireReadConnection();
                    break;
                case AccessMode.Write:
                    conn = AcquireWriteConnection();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown access mode {mode}.");
            }

            if (_disposeCalled)
            {
                ThrowObjectDisposedException();
            }
            return conn;
        }

        public Task<IConnection> AcquireAsync(AccessMode mode)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionError(Uri uri, Exception e)
        {
            _logger?.Info($"Server at {uri} is no longer available due to error: {e.Message}.");
            _routingTable.Remove(uri);
            _clusterConnectionPool.Purge(uri);
        }

        public void OnWriteError(Uri uri)
        {
            _routingTable.RemoveWriter(uri);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;
            _disposeCalled = true;
            // We cannot set routing table and cluster conn pool to null as we do not want get NPE in concurrent call of dispose and acquire
            _routingTable.Clear();
            _clusterConnectionPool.Dispose();

            // cannot set logger to null here otherwise we might concurrent call log and set log to null.
        }

        private void PrependRouters(ISet<Uri> uris)
        {
            _routingTable.PrependRouters(uris);
            _clusterConnectionPool.Add(uris);
        }

        private IConnection AcquireReadConnection()
        {
            EnsureRoutingTableForMode(AccessMode.Read);
            while (true)
            {
                Uri uri;
                if (!_routingTable.TryNextReader(out uri))
                {
                    // no server known to routingTable
                    break;
                }
                IConnection conn = CreateClusterConnection(uri, AccessMode.Read);
                if (conn != null)
                {
                    return conn;
                }
            }
            throw new SessionExpiredException("Failed to connect to any read server.");
        }

        private IConnection AcquireWriteConnection()
        {
            EnsureRoutingTableForMode(AccessMode.Write);
            while (true)
            {
                Uri uri;
                if (!_routingTable.TryNextWriter(out uri))
                {
                    break;
                }

                IConnection conn = CreateClusterConnection(uri);
                if (conn != null)
                {
                    return conn;
                }
            }
            throw new SessionExpiredException("Failed to connect to any write server.");
        }

        private void EnsureRoutingTableForMode(AccessMode mode)
        {
            lock (_syncLock)
            {
                if (!IsRoutingTableStale(_routingTable, mode))
                {
                    return;
                }

                var routingTable = UpdateRoutingTableWithInitialUriFallback();
                _clusterConnectionPool.Update(routingTable.All());
                _routingTable = routingTable;
                _logger?.Info($"Updated routingTable to be {_routingTable}");
            }
        }

        private bool IsRoutingTableStale(IRoutingTable routingTable, AccessMode mode = AccessMode.Read)
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

        internal IRoutingTable UpdateRoutingTableWithInitialUriFallback(
            Func<ISet<Uri>, IRoutingTable> updateRoutingTableFunc = null,
            Func<ISet<Uri>> resolveInitialUriFunc = null)
        {
            lock (_syncLock)
            {
                updateRoutingTableFunc = updateRoutingTableFunc ?? (u => UpdateRoutingTable(null, u));
                resolveInitialUriFunc = resolveInitialUriFunc ?? _seed.Resolve; 

                var hasPrependedInitialRouters = false;
                if (_isReadingInAbsenceOfWriter)
                {
                    PrependRouters(resolveInitialUriFunc());
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
                    var uris = resolveInitialUriFunc();
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

        internal IRoutingTable UpdateRoutingTable(Func<IConnection, IRoutingTable> rediscoveryFunc = null,
            ISet < Uri> triedUris = null)
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
                    IConnection conn = CreateClusterConnection(uri);
                    if (conn != null)
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
                            _logger?.Info($"Failed to update routing table with server uri={uri} due to error {e.Message}");
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

        private ClusterConnection CreateClusterConnection(Uri uri, AccessMode mode = AccessMode.Write)
        {
            lock (_syncLock)
            {
                try
                {
                    IConnection conn;
                    if (_clusterConnectionPool.TryAcquire(uri, out conn))
                    {
                        return new ClusterConnection(conn, uri, mode, this);
                    }
                    OnConnectionError(uri, new ArgumentException(
                        $"Routing table {_routingTable} contains a server {uri} " +
                        $"that is not known to cluster connection pool {_clusterConnectionPool}."));
                }
                catch (ServiceUnavailableException e)
                {
                    OnConnectionError(uri, e);
                }
                return null;
            }
        }

        private IRoutingTable Rediscovery(IConnection conn)
        {
            lock (_syncLock)
            {
                var discoveryManager = new ClusterDiscoveryManager(conn, _routingContext, _logger);
                discoveryManager.Rediscovery();
                return new RoundRobinRoutingTable(discoveryManager.Routers, discoveryManager.Readers,
                    discoveryManager.Writers, _stopwatch, discoveryManager.ExpireAfterSeconds);
            }
        }

        private void ThrowObjectDisposedException()
        {
            FailedToCreateConnection(this);
        }

        internal bool IsReadingInAbsenceOfWriter
        {
            get { return _isReadingInAbsenceOfWriter; }
            set { _isReadingInAbsenceOfWriter = value; }
        }

        public override string ToString()
        {
            return $"{nameof(_routingTable)}: {{{_routingTable}}}, " +
                   $"{nameof(_clusterConnectionPool)}: {{{_clusterConnectionPool}}}";
        }
    }
}