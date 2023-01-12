// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V4_3;

namespace Neo4j.Driver.Internal;

internal interface IBoltProtocolMessageFactory
{
    RunWithMetadataMessage NewRunWithMetadataMessage(IConnection connection, AutoCommitParams autoCommitParams);
    RunWithMetadataMessage NewRunWithMetadataMessage(IConnection connection, Query query);
    PullMessage NewPullMessage(long fetchSize);
    RouteMessage NewRouteMessage(IConnection connection, Bookmarks bookmarks, string database, string impersonatedUser);
    RouteMessageV43 NewRouteMessageV43(IConnection connection, Bookmarks bookmarks, string database);
}
internal class BoltProtocolMessageFactory : IBoltProtocolMessageFactory
{
    public RunWithMetadataMessage NewRunWithMetadataMessage(IConnection connection, AutoCommitParams autoCommitParams)
    {
        return new (
            connection.Version,
            autoCommitParams.Query,
            autoCommitParams.Bookmarks,
            autoCommitParams.Config,
            connection.Mode ?? throw new InvalidOperationException("Connection should have its Mode property set."),
            autoCommitParams.Database,
            autoCommitParams.ImpersonatedUser);
    }

    public RunWithMetadataMessage NewRunWithMetadataMessage(IConnection connection, Query query)
    {
        return new(connection.Version, query);
    }

    public PullMessage NewPullMessage(long fetchSize)
    {
        return new(fetchSize);
    }

    public RouteMessage NewRouteMessage(IConnection connection, Bookmarks bookmarks, string database, string impersonatedUser)
    {
        return new RouteMessage(connection.RoutingContext, bookmarks, database, impersonatedUser);
    }

    public RouteMessageV43 NewRouteMessageV43(
        IConnection connection,
        Bookmarks bookmarks,
        string database)
    {
        return new RouteMessageV43(connection.RoutingContext, bookmarks, database);
    }
}
