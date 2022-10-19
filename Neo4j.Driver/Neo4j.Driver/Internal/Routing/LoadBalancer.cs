// Copyright (c) "Neo4j"
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using static Neo4j.Driver.Internal.Throw.ObjectDisposedException;
using static Neo4j.Driver.Internal.Util.ConnectionContext;

namespace Neo4j.Driver.Internal.Routing
{
    internal class LoadBalancer : IConnectionProvider, IErrorHandler, IClusterConnectionPoolManager
    {
        private readonly IRoutingTableManager _routingTableManager;
        private readonly IInitialServerAddressProvider _initialServerAddressProvider;
        private readonly ILoadBalancingStrategy _loadBalancingStrategy;
        private readonly IClusterConnectionPool _clusterConnectionPool;
        private readonly ILogger _logger;

        private int _closedMarker = 0;

        public RoutingSettings RoutingSetting { get; set; }
        public IDictionary<string, string> RoutingContext { get; set; }

        public LoadBalancer(IPooledConnectionFactory connectionFactory,
            RoutingSettings routingSettings,
            ConnectionPoolSettings poolSettings,
            ILogger logger)
        {
            RoutingSetting = routingSettings;
            RoutingContext = RoutingSetting.RoutingContext;

            _logger = logger;

            _clusterConnectionPool = new ClusterConnectionPool(Enumerable.Empty<Uri>(), connectionFactory, RoutingSetting,  poolSettings, logger);
            _routingTableManager = new RoutingTableManager(routingSettings, this, logger);
            _loadBalancingStrategy = CreateLoadBalancingStrategy(_clusterConnectionPool, _logger);
            _initialServerAddressProvider = routingSettings.InitialServerAddressProvider;
        }

        // for test only
        internal LoadBalancer(
            IClusterConnectionPool clusterConnPool,
            IRoutingTableManager routingTableManager)
        {
            var config = Config.Default;
            _logger = config.Logger;

            _clusterConnectionPool = clusterConnPool;
            _routingTableManager = routingTableManager;
            _loadBalancingStrategy = CreateLoadBalancingStrategy(clusterConnPool, _logger);
        }

        private bool IsClosed => _closedMarker > 0;

        public async Task<IConnection> AcquireAsync(AccessMode mode, string database, string impersonatedUser, Bookmarks bookmarks)
        {
            if (IsClosed)
                throw GetDriverDisposedException(nameof(LoadBalancer));

            var conn = await AcquireConnectionAsync(mode, database, impersonatedUser, bookmarks).ConfigureAwait(false);

            if (IsClosed)
                throw GetDriverDisposedException(nameof(LoadBalancer));

            return conn;
        }

        public Task OnConnectionErrorAsync(Uri uri, string database, Exception e)
        {
            _logger?.Info($"Server at {uri} is no longer available due to error: {e.Message}.");
            _routingTableManager.ForgetServer(uri, database);
            return _clusterConnectionPool.DeactivateAsync(uri);
        }

        public void OnWriteError(Uri uri, string database)
        {
            _routingTableManager.ForgetWriter(uri, database);
        }

        public Task AddConnectionPoolAsync(IEnumerable<Uri> uris)
        {
            return _clusterConnectionPool.AddAsync(uris);
        }

        public Task UpdateConnectionPoolAsync(IEnumerable<Uri> added, IEnumerable<Uri> removed)
        {
            return _clusterConnectionPool.UpdateAsync(added, removed);
        }

        public Task<IConnection> CreateClusterConnectionAsync(Uri uri)
        {
            return CreateClusterConnectionAsync(uri, AccessMode.Write, null, null, Bookmarks.Empty);
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                _routingTableManager.Clear();
                return _clusterConnectionPool.CloseAsync();
            }

            return Task.CompletedTask;
        }

        public async Task<IServerInfo> VerifyConnectivityAndGetInfoAsync()
        {
            try
            {
                var supportsMultiDb = await SupportsMultiDbAsync().ConfigureAwait(false);
                var database = supportsMultiDb ? "system" : null;
                foreach (var uri in _initialServerAddressProvider.Get())
                {
                    return await _routingTableManager.GetServerInfoAsync(uri, database).ConfigureAwait(false);
                }
            }
            catch (ServiceUnavailableException e)
            {
                throw new ServiceUnavailableException(
                    "Unable to connect to database, " +
                    "ensure the database is running and that there is a working network connection to it.", e);
            }

            throw new ServiceUnavailableException(
                "Unable to connect to database, " +
                "ensure the database is running and that there is a working network connection to it.");
        }

        public async Task<bool> SupportsMultiDbAsync()
        {
            var uris = _initialServerAddressProvider.Get();
            await AddConnectionPoolAsync(uris).ConfigureAwait(false);
            var exceptions = new List<Exception>();
            foreach (var uri in uris)
            {
                try
                {
                    var connection = await CreateClusterConnectionAsync(uri, Simple.Mode, Simple.Database, null,
                        Simple.Bookmarks).ConfigureAwait(false);
                    var multiDb = connection.SupportsMultidatabase();
                    await connection.CloseAsync().ConfigureAwait(false);
                    return multiDb;
                }
                catch (SecurityException)
                {
                    throw; // immediately stop
                }
                catch (Exception e)
                {
                    exceptions.Add(e); // save and continue with the next server
                }
            }

            throw new ServiceUnavailableException(
                $"Failed to perform multi-databases feature detection with the following servers: {uris.ToContentString()} ",
                new AggregateException(exceptions));
        }

        public IRoutingTable GetRoutingTable(string database)
        {
            return _routingTableManager.RoutingTableFor(database);
        }

        private async Task<IConnection> AcquireConnectionAsync(AccessMode mode, string database, string impersonatedUser, Bookmarks bookmarks)
        {
            var routingTable = await _routingTableManager.EnsureRoutingTableForModeAsync(mode, database, impersonatedUser, bookmarks)
                .ConfigureAwait(false);

            while (true)
            {
                Uri uri;

                switch (mode)
                {
                    case AccessMode.Read:
                        uri = _loadBalancingStrategy.SelectReader(routingTable.Readers, database);
                        break;
                    case AccessMode.Write:
                        uri = _loadBalancingStrategy.SelectWriter(routingTable.Writers, database);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown access mode {mode}");
                }

                if (uri == null)
                {
                    // no server known to routingTable
                    break;
                }

                var conn = await CreateClusterConnectionAsync(uri, mode, routingTable.Database, impersonatedUser, bookmarks)
                    .ConfigureAwait(false);

                if (conn != null)
                {
                    return conn;
                }

                //else  connection already removed by clusterConnection onError method
            }

            throw new SessionExpiredException($"Failed to connect to any {mode.ToString().ToLower()} server.");
        }

        private async Task<IConnection> CreateClusterConnectionAsync(Uri uri, AccessMode mode, string database, 
            string impersonatedUser, Bookmarks bookmarks)
        {
            try
            {
                var conn = await _clusterConnectionPool.AcquireAsync(uri, mode, database, impersonatedUser, bookmarks)
                    .ConfigureAwait(false);
                if (conn != null)
                {
                    return new ClusterConnection(conn, uri, this);
                }

                await OnConnectionErrorAsync(uri, database, new ArgumentException(
                    $"Routing table {_routingTableManager.RoutingTableFor(database)} contains a server {uri} " +
                    $"that is not known to cluster connection pool {_clusterConnectionPool}.")).ConfigureAwait(false);
            }
            catch (ServiceUnavailableException e)
            {
                await OnConnectionErrorAsync(uri, database, e).ConfigureAwait(false);
            }

            return null;
        }

        public override string ToString()
        {
            return new StringBuilder(128)
                .Append("LoadBalancer{")
                .AppendFormat("routingTableManager={0}, ", _routingTableManager)
                .AppendFormat("clusterConnectionPool={0}, ", _clusterConnectionPool)
                .AppendFormat("closed={0}", IsClosed)
                .Append("}")
                .ToString();
        }

        private static ILoadBalancingStrategy CreateLoadBalancingStrategy(IClusterConnectionPool pool, ILogger logger)
        {
            return new LeastConnectedLoadBalancingStrategy(pool, logger);
        }
    }
}
