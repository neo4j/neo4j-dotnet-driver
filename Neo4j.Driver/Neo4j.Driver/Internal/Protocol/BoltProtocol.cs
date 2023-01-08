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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using V43 = Neo4j.Driver.Internal.Messaging.V4_3;
using V44 = Neo4j.Driver.Internal.Messaging.V4_4;

namespace Neo4j.Driver.Internal;

internal sealed class BoltProtocol : IBoltProtocol
{
    public static readonly IBoltProtocol Instance = new BoltProtocol();
    private readonly LegacyBoltProtocol _legacyProtocol;

    internal BoltProtocol(IBoltProtocolMessageFactory protocolMessageFactory = null,
        IBoltProtocolHandlerFactory protocolHandlerFactory = null)
    {
        _protocolMessageFactory = protocolMessageFactory ?? new BoltProtocolMessageFactory();
        _protocolHandlerFactory = protocolHandlerFactory ?? new BoltProtocolResponseHandlerFactory();
        _legacyProtocol = LegacyBoltProtocol.Instance;
    }

    private readonly IBoltProtocolMessageFactory _protocolMessageFactory;
    private readonly IBoltProtocolHandlerFactory _protocolHandlerFactory;

    public async Task<IResultCursor> RunInAutoCommitTransactionAsync(
        IConnection connection,
        AutoCommitParams autoCommitParams)
    {
        LegacyBoltProtocol.ValidateImpersonatedUserForVersion(connection, autoCommitParams.ImpersonatedUser);

        var summaryBuilder = new SummaryBuilder(autoCommitParams.Query, connection.Server);

        var cursorBuilder = _protocolHandlerFactory.NewResultCursorBuilder(summaryBuilder,
            connection,
            autoCommitParams,
            RequestMore,
            CancelRequest);

        var runMessage = _protocolMessageFactory.NewRunWithMetadataMessage(
            summaryBuilder,
            connection,
            autoCommitParams);

        var runHandler = _protocolHandlerFactory.NewRunHandler(cursorBuilder, summaryBuilder);
        await connection.EnqueueAsync(runMessage, runHandler).ConfigureAwait(false);
        
        if (autoCommitParams.Reactive)
        {
            var pullMessage = new PullMessage(autoCommitParams.FetchSize);
            var pullHandler = new PullResponseHandler(cursorBuilder, summaryBuilder, autoCommitParams.BookmarksTracker);

            await connection.EnqueueAsync(pullMessage, pullHandler).ConfigureAwait(false);
        }

        await connection.SendAsync().ConfigureAwait(false);

        return cursorBuilder.CreateCursor();
    }

    public Task BeginTransactionAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser)
    {
        LegacyBoltProtocol.ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        return _legacyProtocol.BeginTransactionAsync(connection, database, bookmarks, config, impersonatedUser);
    }

    public async Task<IResultCursor> RunInExplicitTransactionAsync(
        IConnection connection,
        Query query,
        bool reactive,
        long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(
            summaryBuilder,
            connection.ReceiveOneAsync,
            RequestMore(connection, summaryBuilder, null),
            CancelRequest(connection, summaryBuilder, null),
            null,
            fetchSize,
            reactive);

        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);

        var pullMessage = reactive ? null : new PullMessage(fetchSize);
        var pullHandler = reactive ? null : new PullResponseHandler(streamBuilder, summaryBuilder, null);

        await connection.EnqueueAsync(
                new RunWithMetadataMessage(connection.Version, query),
                runHandler)
            .ConfigureAwait(false);
        await connection.EnqueueAsync(
            pullMessage,
            pullHandler).ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
    {
        return _legacyProtocol.CommitTransactionAsync(connection, bookmarksTracker);
    }

    public Task RollbackTransactionAsync(IConnection connection)
    {
        return _legacyProtocol.RollbackTransactionAsync(connection);
    }

    public Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
    {
        return _legacyProtocol.LoginAsync(connection, userAgent, authToken);
    }

    public Task LogoutAsync(IConnection connection)
    {
        return _legacyProtocol.LogoutAsync(connection);
    }

    public Task ResetAsync(IConnection connection)
    {
        return _legacyProtocol.ResetAsync(connection);
    }

    public Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        IConnection connection,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        connection = connection ??
            throw new ProtocolException("Attempting to get a routing table on a null connection");

        LegacyBoltProtocol.ValidateImpersonatedUserForVersion(connection, impersonatedUser);

        return connection.Version >= BoltProtocolVersion.V4_3
            ? GetRoutingTableWithRouteMessageAsync(connection, database, impersonatedUser, bookmarks)
            : GetRoutingTableWithQueryAsync(connection, database, bookmarks);
    }

    private async Task<IReadOnlyDictionary<string, object>> GetRoutingTableWithQueryAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks)
    {
        connection.ConfigureMode(AccessMode.Read);

        var bookmarkTracker = new BookmarksTracker(bookmarks);
        var resourceHandler = new ConnectionResourceHandler(connection);
        var databaseParameter = string.IsNullOrEmpty(database) ? null : database;
        var autoCommitParams = new AutoCommitParams
        {
            Query = new Query(
                "CALL dbms.routing.getRoutingTable($context, $database)",
                new Dictionary<string, object>
                {
                    ["context"] = connection.RoutingContext,
                    ["database"] = databaseParameter
                }),
            BookmarksTracker = bookmarkTracker,
            ResultResourceHandler = resourceHandler,
            Database = "system",
            Bookmarks = bookmarks
        };

        var result = await RunInAutoCommitTransactionAsync(connection, autoCommitParams).ConfigureAwait(false);
        var record = await result.SingleAsync().ConfigureAwait(false);

        //Since 4.4 the Routing information will contain a db.
        //Earlier versions need to populate this here as it's not received in the older route response...
        var finalDictionary = record.Values.ToDictionary();
        finalDictionary["db"] = database;

        return (IReadOnlyDictionary<string, object>)finalDictionary;
    }

    private async Task<IReadOnlyDictionary<string, object>> GetRoutingTableWithRouteMessageAsync(
        IConnection connection,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        IRequestMessage message = connection.Version == BoltProtocolVersion.V4_3
            ? new V43.RouteMessage(connection.RoutingContext, bookmarks, database)
            : new V44.RouteMessage(connection.RoutingContext, bookmarks, database, impersonatedUser);

        var responseHandler = new RouteResponseHandler();

        await connection.EnqueueAsync(message, responseHandler).ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);

        // Since 4.4 the Routing information will contain a db.
        // 4.3 needs to populate this here as it's not received in the older route response...
        if (connection.Version == BoltProtocolVersion.V4_3)
        {
            responseHandler.RoutingInformation.Add("db", database);
        }

        return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
    }

    private static Func<IResultStreamBuilder, long, long, Task> RequestMore(
        IConnection connection,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id, n) =>
        {
            var pullResponseHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            await connection
                .EnqueueAsync(new PullMessage(id, n), pullResponseHandler)
                .ConfigureAwait(false);

            await connection.SendAsync().ConfigureAwait(false);
        };
    }

    private static Func<IResultStreamBuilder, long, Task> CancelRequest(
        IConnection connection,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id) =>
        {
            var pullResponseHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            await connection
                .EnqueueAsync(new DiscardMessage(id, ResultHandleMessage.All), pullResponseHandler)
                .ConfigureAwait(false);

            await connection.SendAsync().ConfigureAwait(false);
        };
    }
}
