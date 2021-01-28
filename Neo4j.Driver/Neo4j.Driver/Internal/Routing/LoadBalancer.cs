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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;
using static Neo4j.Driver.Internal.Throw.ObjectDisposedException;
using static Neo4j.Driver.Internal.Util.ConnectionContext;

namespace Neo4j.Driver.Internal.Routing
{
    internal class LoadBalancer : IConnectionProvider, IClusterErrorHandler, IClusterConnectionPoolManager
    {
        private readonly IRoutingTableManager _routingTableManager;
        private readonly ILoadBalancingStrategy _loadBalancingStrategy;
        private readonly IClusterConnectionPool _clusterConnectionPool;
        private readonly ILogger _logger;

        private int _closedMarker = 0;
        private IInitialServerAddressProvider _initialServerAddressProvider;

        public RoutingSettings RoutingSetting { get; set; }
        public IDictionary<string, string> RoutingContext { get; set; }

        public LoadBalancer(
            IPooledConnectionFactory connectionFactory,
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

        public async Task<IConnection> AcquireAsync(AccessMode mode, string database, Bookmark bookmark)
        {
            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }

            var conn = await AcquireConnectionAsync(mode, database, bookmark).ConfigureAwait(false);

            if (IsClosed)
            {
                ThrowObjectDisposedException();
            }

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
            return CreateClusterConnectionAsync(uri, AccessMode.Write, null, Bookmark.Empty);
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

        public async Task VerifyConnectivityAsync()
        {
            // As long as there is a fresh routing table, we consider we can route to these servers.
            try
            {
                var database = await SupportsMultiDbAsync().ConfigureAwait(false) ? "system" : null;
                await _routingTableManager.EnsureRoutingTableForModeAsync(Simple.Mode, database,
                    Simple.Bookmark).ConfigureAwait(false);
            }
            catch (ServiceUnavailableException e)
            {
                throw new ServiceUnavailableException(
                    "Unable to connect to database, " +
                    "ensure the database is running and that there is a working network connection to it.", e);
            }
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
                    var connection = await CreateClusterConnectionAsync(uri, Simple.Mode, Simple.Database,
                        Simple.Bookmark).ConfigureAwait(false);
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

        public async Task<IConnection> AcquireConnectionAsync(AccessMode mode, string database, Bookmark bookmark)
        {
            var routingTable = await _routingTableManager.EnsureRoutingTableForModeAsync(mode, database, bookmark)
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

                var conn =
                    await CreateClusterConnectionAsync(uri, mode, database, bookmark).ConfigureAwait(false);
                if (conn != null)
                {
                    return conn;
                }

                //else  connection already removed by clusterConnection onError method
            }

            throw new SessionExpiredException($"Failed to connect to any {mode.ToString().ToLower()} server.");
        }

        private async Task<IConnection> CreateClusterConnectionAsync(Uri uri, AccessMode mode, string database,
            Bookmark bookmark)
        {
            try
            {
                var conn = await _clusterConnectionPool.AcquireAsync(uri, mode, database, bookmark)
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

        private void ThrowObjectDisposedException()
        {
            FailedToAcquireConnection(this);
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

        private static ILoadBalancingStrategy CreateLoadBalancingStrategy(IClusterConnectionPool pool,
            ILogger logger)
        {
            return new LeastConnectedLoadBalancingStrategy(pool, logger);
        }
    }
}