// Copyright (c) 2002-2020 "Neo4j,"
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
using Neo4j.Driver;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Routing
{
    internal class ClusterDiscovery : IDiscovery
    {
        private readonly ILogger _logger;
        private readonly IDictionary<string, string> _context;

        private const string GetRoutingTableProcedure = "CALL dbms.cluster.routing.getRoutingTable($context)";

        private const string GetRoutingTableForDatabaseProcedure = "CALL dbms.routing.getRoutingTable($context, $database)";


        public ClusterDiscovery(IDictionary<string, string> context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        internal Query DiscoveryProcedure(IConnection connection, string database)  //TODO: ROUTE - Replace with route call and response  for 4.3 and up
        {
            if (connection.SupportsMultidatabase())
            {
                return new Query(GetRoutingTableForDatabaseProcedure,
                    new Dictionary<string, object>
                        {{"context", _context}, {"database", string.IsNullOrEmpty(database) ? null : database}});
            }

            return new Query(GetRoutingTableProcedure,
                new Dictionary<string, object> {{"context", _context}});
        }

        /// <remarks>Throws <see cref="ProtocolException"/> if the discovery result is invalid.</remarks>
        /// <remarks>Throws <see cref="ServiceUnavailableException"/> if the no discovery procedure could be found in the server.</remarks>
        public async Task<IRoutingTable> DiscoverAsync(IConnection connection, string database, Bookmark bookmark)
        {
            RoutingTable table;

            var provider = new SingleConnectionBasedConnectionProvider(connection);
            var multiDb = connection.SupportsMultidatabase();
            var sessionAccessMode = multiDb ? AccessMode.Read : AccessMode.Write;
            var sessionDb = multiDb ? "system" : null;
            var session = new AsyncSession(provider, _logger, null, sessionAccessMode, sessionDb, bookmark);
            try
            {
                var stmt = DiscoveryProcedure(connection, database);
                var result = await session.RunAsync(stmt).ConfigureAwait(false);    //TODO: ROUTE - Message should go here I think...
                var record = await result.SingleAsync().ConfigureAwait(false);

                table = ParseDiscoveryResult(database, record);
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

        private static RoutingTable ParseDiscoveryResult(string database, IRecord record)
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

            return new RoutingTable(database, routers, readers, writers, record["ttl"].As<long>());
        }

        public static Uri BoltRoutingUri(string address)
        {
            UriBuilder builder = new UriBuilder("neo4j://" + address);

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
            public IDictionary<string, string> RoutingContext { get; set; }

            public SingleConnectionBasedConnectionProvider(IConnection connection)
            {
                _connection = connection;
                RoutingContext = connection.RoutingContext;
            }

            public Task<IConnection> AcquireAsync(AccessMode mode, string database, Bookmark bookmark)
            {
                var conn = _connection;
                conn.Mode = mode;
                conn.Database = database;
                _connection = null;
                return Task.FromResult(conn);
            }

            public Task CloseAsync()
            {
                if (_connection != null)
                {
                    return _connection.CloseAsync();
                }

                return Task.CompletedTask;
            }

            public Task VerifyConnectivityAsync()
            {
                throw new NotSupportedException();
            }

            public Task<bool> SupportsMultiDbAsync()
            {
                throw new NotSupportedException();
            }
        }
    }
}