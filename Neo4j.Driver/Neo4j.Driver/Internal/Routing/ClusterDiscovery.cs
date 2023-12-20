// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Routing;

internal class ClusterDiscovery : IDiscovery
{
    /// <remarks>Throws <see cref="ProtocolException"/> if the discovery result is invalid.</remarks>
    /// <remarks>Throws <see cref="ServiceUnavailableException"/> if the no discovery procedure could be found in the server.</remarks>
    public async Task<IRoutingTable> DiscoverAsync(
        IConnection connection,
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks)
    {
        var routingTable = await connection.GetRoutingTableAsync(database, sessionConfig, bookmarks)
            .ConfigureAwait(false);

        return ParseDiscoveryResult(routingTable);
    }

    private static RoutingTable ParseDiscoveryResult(IReadOnlyDictionary<string, object> routingTable)
    {
        var routers = default(Uri[]);
        var readers = default(Uri[]);
        var writers = default(Uri[]);

        foreach (var servers in routingTable["servers"].As<List<Dictionary<string, object>>>())
        {
            var addresses = servers["addresses"].As<List<string>>();
            var role = servers["role"].As<string>();
            switch (role)
            {
                case "READ":
                    readers = addresses.Select(Neo4jUri.BoltRoutingUri).ToArray();
                    break;

                case "WRITE":
                    writers = addresses.Select(Neo4jUri.BoltRoutingUri).ToArray();
                    break;

                case "ROUTE":
                    routers = addresses.Select(Neo4jUri.BoltRoutingUri).ToArray();
                    break;

                default:
                    throw new ProtocolException(
                        $"Role '{role}' returned from discovery procedure is not recognized by the driver");
            }
        }

        if (IsInvalidDiscoveryResult(readers, routers))
        {
            throw new ProtocolException(
                $"Invalid discovery result: discovered {routers?.Length ?? 0} routers, {writers?.Length ?? 0} writers and {readers?.Length ?? 0} readers.");
        }

        routingTable.TryGetValue("db", out var db);
        return new RoutingTable((string)db, routers, readers, writers, routingTable["ttl"].As<long>());
    }

    private static bool IsInvalidDiscoveryResult(Uri[] readers, Uri[] routers)
    {
        return readers?.Length == 0 || routers.Length == 0;
    }
}
