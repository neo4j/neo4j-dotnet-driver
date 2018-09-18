// Copyright (c) 2002-2018 "Neo4j,"
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
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Throw.ObjectDisposedException;

namespace Neo4j.Driver.Internal.Routing
{
    internal class LoadBalancer : IConnectionProvider, IClusterErrorHandler, IClusterConnectionPoolManager
    {
        private readonly IRoutingTableManager _routingTableManager;
        private readonly ILoadBalancingStrategy _loadBalancingStrategy;
        private readonly IClusterConnectionPool _clusterConnectionPool;
        private readonly IDriverLogger _logger;

        private int _closedMarker = 0;

        public LoadBalancer(
            IPooledConnectionFactory connectionFactory,
            RoutingSettings routingSettings,
            ConnectionPoolSettings poolSettings,
            IDriverLogger logger)
        {
            _logger = logger;

            _clusterConnectionPool = new ClusterConnectionPool(Enumerable.Empty<Uri>(), connectionFactory, poolSettings, logger);
            _routingTableManager = new RoutingTableManager(routingSettings, this, logger);
            _loadBalancingStrategy = CreateLoadBalancingStrategy(routingSettings.Strategy, _clusterConnectionPool, _logger);
        }

        // for test only
        internal LoadBalancer(
            IClusterConnectionPool clusterConnPool,
            IRoutingTableManager routingTableManager)
        {
            var config = Config.DefaultConfig;
            _logger = config.DriverLogger;

            _clusterConnectionPool = clusterConnPool;
            _routingTableManager = routingTableManager;
            _loadBalancingStrategy = CreateLoadBalancingStrategy(config.LoadBalancingStrategy, clusterConnPool, _logger);
        }

        private bool IsClosed => _closedMarker > 0;

        public IConnection Acquire(AccessMode mode)
        {
            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }

            IConnection conn = AcquireConnection(mode);

            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }
            return conn;
        }

        public async Task<IConnection> AcquireAsync(AccessMode mode)
        {
            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }

            IConnection conn = await AcquireConnectionAsync(mode).ConfigureAwait(false);

            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }
            return conn;
        }

        public void OnConnectionError(Uri uri, Exception e)
        {
            _logger?.Info($"Server at {uri} is no longer available due to error: {e.Message}.");
            _routingTableManager.RoutingTable.Remove(uri);
            _clusterConnectionPool.Deactivate(uri);
        }

        public Task OnConnectionErrorAsync(Uri uri, Exception e)
        {
            _logger?.Info($"Server at {uri} is no longer available due to error: {e.Message}.");
            _routingTableManager.RoutingTable.Remove(uri);
            return _clusterConnectionPool.DeactivateAsync(uri);
        }

        public void OnWriteError(Uri uri)
        {
            _routingTableManager.RoutingTable.RemoveWriter(uri);
        }

        public void AddConnectionPool(IEnumerable<Uri> uris)
        {
            _clusterConnectionPool.Add(uris);
        }

        public Task AddConnectionPoolAsync(IEnumerable<Uri> uris)
        {
            return _clusterConnectionPool.AddAsync(uris);
        }

        public void UpdateConnectionPool(IEnumerable<Uri> added, IEnumerable<Uri> removed)
        {
            _clusterConnectionPool.Update(added, removed);
        }

        public Task UpdateConnectionPoolAsync(IEnumerable<Uri> added, IEnumerable<Uri> removed)
        {
            return _clusterConnectionPool.UpdateAsync(added, removed);
        }

        public IConnection CreateClusterConnection(Uri uri)
        {
            return CreateClusterConnection(uri, AccessMode.Write);
        }

        public Task<IConnection> CreateClusterConnectionAsync(Uri uri)
        {
            return CreateClusterConnectionAsync(uri, AccessMode.Write);
;       }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                _routingTableManager.RoutingTable.Clear();
                _clusterConnectionPool.Close();
            }
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                _routingTableManager.RoutingTable.Clear();
                return _clusterConnectionPool.CloseAsync();
            }

            return TaskHelper.GetCompletedTask();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        public IConnection AcquireConnection(AccessMode mode)
        {
            _routingTableManager.EnsureRoutingTableForMode(mode);
            while (true)
            {
                Uri uri;

                switch (mode)
                {
                    case AccessMode.Read:
                        uri = _loadBalancingStrategy.SelectReader(_routingTableManager.RoutingTable.Readers);
                        break;
                    case AccessMode.Write:
                        uri = _loadBalancingStrategy.SelectWriter(_routingTableManager.RoutingTable.Writers);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown access mode {mode}");
                }

                if (uri == null)
                {
                    // no server known to routingTable
                    break;
                }
                IConnection conn = CreateClusterConnection(uri, mode);
                if (conn != null)
                {
                    return conn;
                }
                //else  connection already removed by clusterConnection onError method
            }
            throw new SessionExpiredException($"Failed to connect to any {mode.ToString().ToLower()} server.");
        }

        public async Task<IConnection> AcquireConnectionAsync(AccessMode mode)
        {
            await _routingTableManager.EnsureRoutingTableForModeAsync(mode).ConfigureAwait(false);
            while (true)
            {
                Uri uri;

                switch (mode)
                {
                    case AccessMode.Read:
                        uri = _loadBalancingStrategy.SelectReader(_routingTableManager.RoutingTable.Readers);
                        break;
                    case AccessMode.Write:
                        uri = _loadBalancingStrategy.SelectWriter(_routingTableManager.RoutingTable.Writers);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown access mode {mode}");
                }

                if (uri == null)
                {
                    // no server known to routingTable
                    break;
                }
                IConnection conn = await CreateClusterConnectionAsync(uri, mode).ConfigureAwait(false);
                if (conn != null)
                {
                    return conn;
                }
                //else  connection already removed by clusterConnection onError method
            }
            throw new SessionExpiredException($"Failed to connect to any {mode.ToString().ToLower()} server.");
        }

        private IConnection CreateClusterConnection(Uri uri, AccessMode mode)
        {
            try
            {
                IConnection conn = _clusterConnectionPool.Acquire(uri);
                if (conn != null)
                {
                    return new ClusterConnection(conn, uri, mode, this);
                }
                OnConnectionError(uri, new ArgumentException(
                    $"Routing table {_routingTableManager.RoutingTable} contains a server '{uri}' " +
                    $"that is not known to cluster connection pool {_clusterConnectionPool}."));
            }
            catch (ServiceUnavailableException e)
            {
                OnConnectionError(uri, e);
            }
            return null;
        }

        private async Task<IConnection> CreateClusterConnectionAsync(Uri uri, AccessMode mode)
        {
            try
            {
                IConnection conn = await _clusterConnectionPool.AcquireAsync(uri).ConfigureAwait(false);
                if (conn != null)
                {
                    return new ClusterConnection(conn, uri, mode, this);
                }
                await OnConnectionErrorAsync(uri, new ArgumentException(
                    $"Routing table {_routingTableManager.RoutingTable} contains a server {uri} " +
                    $"that is not known to cluster connection pool {_clusterConnectionPool}.")).ConfigureAwait(false);
            }
            catch (ServiceUnavailableException e)
            {
                await OnConnectionErrorAsync(uri, e).ConfigureAwait(false);
            }
            return null;
        }

        private void ThrowObjectDisposedException()
        {
            FailedToAcquireConnection(this);
        }

        public override string ToString()
        {
            return $"{nameof(_routingTableManager.RoutingTable)}: {{{_routingTableManager.RoutingTable}}}, " +
                   $"{nameof(_clusterConnectionPool)}: {{{_clusterConnectionPool}}}";
        }

        private static ILoadBalancingStrategy CreateLoadBalancingStrategy(LoadBalancingStrategy strategy, IClusterConnectionPool pool,
            IDriverLogger logger)
        {
            if (strategy == LoadBalancingStrategy.LeastConnected)
            {
                return new LeastConnectedLoadBalancingStrategy(pool, logger);
            }
            else if (strategy == LoadBalancingStrategy.RoundRobin)
            {
                return new RoundRobinLoadBalancingStrategy(logger);
            }
            else
            {
                throw new ArgumentException($"Unknown load balancing strategy: {strategy}");
            }
        }
    }
}
