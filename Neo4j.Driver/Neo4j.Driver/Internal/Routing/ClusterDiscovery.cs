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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterDiscovery : IDiscovery
    {
        private readonly IDriverLogger _logger;
        private readonly IDictionary<string, string> _context;

        private const string GetServersProcedure = "dbms.cluster.routing.getServers";
        private const string GetRoutingTableProcedure = "dbms.cluster.routing.getRoutingTable";

        public ClusterDiscovery(IDictionary<string, string> context, IDriverLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        internal Statement DiscoveryProcedure(IConnection connection)
        {
            if (ServerVersion.Version(connection.Server.Version) >= ServerVersion.V3_2_0)
            {
                return new Statement($"CALL {GetRoutingTableProcedure}({{context}})",
                    new Dictionary<string, object> {{"context", _context}});
            }
            else
            {
                return new Statement($"CALL {GetServersProcedure}");
            }
        }

        /// <remarks>Throws <see cref="ProtocolException"/> if the discovery result is invalid.</remarks>
        /// <remarks>Throws <see cref="ServiceUnavailableException"/> if the no discovery procedure could be found in the server.</remarks>
        public IRoutingTable Discover(IConnection connection)
        {
            var table = default(RoutingTable);

            try
            {
                using (var provider = new SingleConnectionBasedConnectionProvider(connection))
                using (var session = new Session(provider, _logger))
                {
                    var result = session.Run(DiscoveryProcedure(connection));
                    var record = result.Single();

                    table = ParseDiscoveryResult(record);
                }
            }
            catch (Exception e)
            {
                HandleDiscoveryException(e);
            }

            return table;
        }

        public async Task<IRoutingTable> DiscoverAsync(IConnection connection)
        {
            var table = default(RoutingTable);

            var provider = new SingleConnectionBasedConnectionProvider(connection);
            var session = new Session(provider, _logger);
            try
            {
                var result = await session.RunAsync(DiscoveryProcedure(connection)).ConfigureAwait(false);
                var record = await result.SingleAsync().ConfigureAwait(false);

                table = ParseDiscoveryResult(record);
            }
            catch (Exception e)
            {
                HandleDiscoveryException(e);
            }
            finally
            {
                try
                {
                    await session.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignore any exception
                }

                await provider.CloseAsync().ConfigureAwait(false);
            }

            return table;
        }

        private void HandleDiscoveryException(Exception e)
        {
            throw new ServiceUnavailableException($"Error performing discovery: {e.Message}.", e);
        }

        private static RoutingTable ParseDiscoveryResult(IRecord record)
        {
            var routers = default(Uri[]);
            var readers = default(Uri[]);
            var writers = default(Uri[]);

            foreach (var servers in record["servers"].As<List<Dictionary<string, object>>>())
            {
                var addresses = servers["addresses"].As<List<string>>();
                var role = servers["role"].As<string>();
                switch (role)
                {
                    case "READ":
                        readers = addresses.Select(BoltRoutingUri).ToArray();
                        break;
                    case "WRITE":
                        writers = addresses.Select(BoltRoutingUri).ToArray();
                        break;
                    case "ROUTE":
                        routers = addresses.Select(BoltRoutingUri).ToArray();
                        break;
                    default:
                        throw new ProtocolException(
                            $"Role '{role}' returned from discovery procedure is not recognized by the driver");
                }
            }

            if ((readers == null || readers.Length == 0) || (routers == null || routers.Length == 0))
            {
                throw new ProtocolException(
                    $"Invalid discovery result: discovered {routers?.Length ?? 0} routers, {writers?.Length ?? 0} writers and {readers?.Length ?? 0} readers.");
            }

            return new RoutingTable(routers, readers, writers, record["ttl"].As<long>());
        }

        public static Uri BoltRoutingUri(string address)
        {
            UriBuilder builder = new UriBuilder("bolt+routing://" + address);

            // If scheme is not registered and no port is specified, then the port is assigned as -1
            if (builder.Port == -1)
            {
                builder.Port = GraphDatabase.DefaultBoltPort;
            }

            return builder.Uri;
        }

        private class SingleConnectionBasedConnectionProvider : IConnectionProvider
        {
            private IConnection _connection;

            public SingleConnectionBasedConnectionProvider(IConnection connection)
            {
                _connection = connection;
            }

            public void Dispose()
            {
                _connection?.Close();
            }

            public IConnection Acquire(AccessMode mode)
            {
                var conn = _connection;
                conn.Mode = mode;
                _connection = null;
                return conn;
            }

            public Task<IConnection> AcquireAsync(AccessMode mode)
            {
                return Task.FromResult(Acquire(mode));
            }

            public void Close()
            {
                _connection?.Close();
            }

            public Task CloseAsync()
            {
                if (_connection != null)
                {
                    return _connection.CloseAsync();
                }

                return TaskHelper.GetCompletedTask();
            }
        }
    }
}