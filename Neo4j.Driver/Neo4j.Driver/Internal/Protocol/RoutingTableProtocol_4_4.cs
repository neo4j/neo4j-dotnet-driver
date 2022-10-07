// Copyright (c) 2002-2022 "Neo4j,"
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using Neo4j.Driver.Internal.Messaging.V4_4;

namespace Neo4j.Driver.Internal.Protocol;

internal class RoutingTableProtocol_4_4 : IRoutingTableProtocol
{
    public async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection,
        string database, string impersonatedUser, Bookmarks bookmarks)
    {
        connection = connection ??
                     throw new ProtocolException("Attempting to get a routing table on a null connection");

        var responseHandler = new RouteResponseHandler();

        await connection.EnqueueAsync(
                new RouteMessage(connection.RoutingContext, bookmarks, database, impersonatedUser),
                responseHandler)
            .ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
        await connection.CloseAsync().ConfigureAwait(false);

        return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
    }
}