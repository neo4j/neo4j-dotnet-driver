﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Telemetry;

namespace Neo4j.Driver.Internal.Protocol;

internal interface IBoltProtocolMessageFactory
{
    RunWithMetadataMessage NewRunWithMetadataMessage(
        IConnection connection,
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig);

    RunWithMetadataMessage NewRunWithMetadataMessage(
        IConnection connection,
        Query query,
        INotificationsConfig notificationsConfig);

    PullMessage NewPullMessage(long fetchSize);
    PullMessage NewPullMessage(long id, long fetchSize);
    RouteMessage NewRouteMessage(IConnection connection, Bookmarks bookmarks, string database, string impersonatedUser);
    RouteMessageV43 NewRouteMessageV43(IConnection connection, Bookmarks bookmarks, string database);
    DiscardMessage NewDiscardMessage(long id, long discardSize);
    HelloMessage NewHelloMessage(IConnection connection, string userAgent, IAuthToken authToken);
    LogonMessage NewLogonMessage(IConnection connection, IAuthToken authToken);

    HelloMessage NewAuthlessHelloMessage(
        IConnection connection,
        string userAgent,
        INotificationsConfig notificationsConfig);

    BeginMessage NewBeginMessage(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        AccessMode mode,
        INotificationsConfig notificationsConfig);

    TelemetryMessage NewTelemetryMessage(IConnection connection, TransactionInfo info);
}

internal class BoltProtocolMessageFactory : IBoltProtocolMessageFactory
{
    internal static readonly BoltProtocolMessageFactory Instance = new();

    public RunWithMetadataMessage NewRunWithMetadataMessage(
        IConnection connection,
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig)
    {
        return new RunWithMetadataMessage(
            connection.Version,
            autoCommitParams.Query,
            autoCommitParams.Bookmarks,
            autoCommitParams.Config,
            connection.Mode ?? throw new InvalidOperationException("Connection should have its Mode property set."),
            autoCommitParams.Database,
            autoCommitParams.SessionConfig,
            notificationsConfig);
    }

    public RunWithMetadataMessage NewRunWithMetadataMessage(
        IConnection connection,
        Query query,
        INotificationsConfig notificationsConfig)
    {
        return new RunWithMetadataMessage(connection.Version, query, notificationsConfig: notificationsConfig);
    }

    public PullMessage NewPullMessage(long fetchSize)
    {
        return new PullMessage(fetchSize);
    }

    public PullMessage NewPullMessage(long id, long fetchSize)
    {
        return new PullMessage(id, fetchSize);
    }

    public RouteMessage NewRouteMessage(
        IConnection connection,
        Bookmarks bookmarks,
        string database,
        string impersonatedUser)
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

    public DiscardMessage NewDiscardMessage(long id, long discardSize)
    {
        return new DiscardMessage(id, discardSize);
    }

    public HelloMessage NewHelloMessage(
        IConnection connection,
        string userAgent,
        IAuthToken authToken)
    {
        var authDict = connection.Version >= BoltProtocolVersion.V5_1
            ? null
            : authToken.AsDictionary();

        return new HelloMessage(
            connection.Version,
            userAgent,
            authDict,
            connection.RoutingContext);
    }

    public HelloMessage NewAuthlessHelloMessage(
        IConnection connection,
        string userAgent,
        INotificationsConfig notificationsConfig)
    {
        return new HelloMessage(connection.Version, userAgent, connection.RoutingContext, notificationsConfig);
    }

    public LogonMessage NewLogonMessage(IConnection connection, IAuthToken authToken)
    {
        return new LogonMessage(authToken);
    }

    public BeginMessage NewBeginMessage(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        AccessMode mode,
        INotificationsConfig notificationsConfig)
    {
        return new BeginMessage(
            connection.Version,
            database,
            bookmarks,
            config,
            mode,
            connection.SessionConfig,
            notificationsConfig);
    }

    public TelemetryMessage NewTelemetryMessage(IConnection connection, TransactionInfo info)
    {
        return new TelemetryMessage(info.ApiType);
    }
}
