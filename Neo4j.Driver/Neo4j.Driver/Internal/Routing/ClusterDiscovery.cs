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
using Neo4j.Driver.Internal.MessageHandling;

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

        /// <remarks>Throws <see cref="ProtocolException"/> if the discovery result is invalid.</remarks>
        /// <remarks>Throws <see cref="ServiceUnavailableException"/> if the no discovery procedure could be found in the server.</remarks>
        public async Task<IRoutingTable> DiscoverAsync(IConnection connection, string database, Bookmark bookmark)
        {
            var bookmarkTracker = new BookmarkTracker(bookmark);
            var resourceHandler = new ConnectionResourceHandler(connection);
            var sessionDb = connection.SupportsMultidatabase() ? "system" : null;
            var result = await connection.BoltProtocol.GetRoutingTable(connection, database, sessionDb, resourceHandler, bookmarkTracker, bookmark);   //Not ideal passing the connection in... but protocol currently doesn't know what connection it is on. Needs some though...
            var record = await result.SingleAsync().ConfigureAwait(false);
            
            return ParseDiscoveryResult(database, record);
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

        private class ConnectionResourceHandler : IResultResourceHandler
        {
            IConnection Connection { get; }
            public ConnectionResourceHandler(IConnection conn)
            {
                Connection = conn;
            }

            public Task OnResultConsumedAsync()
            {
                return CloseConnection();
            }

            private async Task CloseConnection()
            {
                await Connection.CloseAsync().ConfigureAwait(false);
            }
        }

		private class BookmarkTracker : IBookmarkTracker
        {
            private Bookmark InternalBookmark { get; set; }

            public BookmarkTracker(Bookmark bookmark)
			{
                InternalBookmark = bookmark;
            }

            public void UpdateBookmark(Bookmark bookmark)
            {
                if (InternalBookmark != null && InternalBookmark.Values.Any())
                {
                    InternalBookmark = bookmark;
                }
            }
        }

    }
}