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
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal;

internal sealed class BoltProtocol : IBoltProtocol
{
    internal static readonly IBoltProtocol Instance = new BoltProtocol();
    private readonly IBoltProtocol _boltProtocolV3;
    private readonly IBoltProtocolHandlerFactory _protocolHandlerFactory;

    private readonly IBoltProtocolMessageFactory _protocolMessageFactory;

    internal BoltProtocol(
        IBoltProtocol boltProtocolV3 = null,
        IBoltProtocolMessageFactory protocolMessageFactory = null,
        IBoltProtocolHandlerFactory protocolHandlerFactory = null)
    {
        _protocolMessageFactory = protocolMessageFactory ?? BoltProtocolMessageFactory.Instance;
        _protocolHandlerFactory = protocolHandlerFactory ?? BoltProtocolHandlerFactory.Instance;
        _boltProtocolV3 = boltProtocolV3 ?? BoltProtocolV3.Instance;
    }

    public Task LoginAsync(
        IConnection connection,
        string userAgent,
        IAuthToken authToken,
        INotificationsConfig notificationsConfig)
    {
        return connection.Version < BoltProtocolVersion.V5_1
            ? _boltProtocolV3.LoginAsync(connection, userAgent, authToken, null)
            : LoginV51Async(connection, userAgent, authToken, notificationsConfig);
    }

    public Task LogoutAsync(IConnection connection)
    {
        return _boltProtocolV3.LogoutAsync(connection);
    }

    public Task ResetAsync(IConnection connection)
    {
        return _boltProtocolV3.ResetAsync(connection);
    }

    public Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        IConnection connection,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        connection = connection ??
            throw new ProtocolException("Attempting to get a routing table on a null connection");

        BoltProtocolV3.ValidateImpersonatedUserForVersion(connection, impersonatedUser);

        return connection.Version >= BoltProtocolVersion.V4_3
            ? GetRoutingTableWithRouteMessageAsync(connection, database, impersonatedUser, bookmarks)
            : GetRoutingTableWithQueryAsync(connection, database, bookmarks);
    }

    public async Task<IResultCursor> RunInAutoCommitTransactionAsync(
        IConnection connection,
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig)
    {
        BoltProtocolV3.ValidateImpersonatedUserForVersion(connection, autoCommitParams.ImpersonatedUser);

        var summaryBuilder = new SummaryBuilder(autoCommitParams.Query, connection.Server);

        var streamBuilder = _protocolHandlerFactory.NewResultCursorBuilder(
            summaryBuilder,
            connection,
            RequestMore,
            CancelRequest,
            autoCommitParams.BookmarksTracker,
            autoCommitParams.ResultResourceHandler,
            autoCommitParams.FetchSize,
            autoCommitParams.Reactive);

        var runMessage = _protocolMessageFactory.NewRunWithMetadataMessage(connection, autoCommitParams);
        var runHandler = _protocolHandlerFactory.NewRunResponseHandler(streamBuilder, summaryBuilder);

        await connection.EnqueueAsync(runMessage, runHandler).ConfigureAwait(false);

        if (!autoCommitParams.Reactive)
        {
            var pullMessage = _protocolMessageFactory.NewPullMessage(autoCommitParams.FetchSize);
            var pullHandler = _protocolHandlerFactory.NewPullResponseHandler(
                autoCommitParams.BookmarksTracker,
                streamBuilder,
                summaryBuilder);

            await connection.EnqueueAsync(pullMessage, pullHandler).ConfigureAwait(false);
        }

        await connection.SendAsync().ConfigureAwait(false);

        return streamBuilder.CreateCursor();
    }

    public Task BeginTransactionAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser,
        INotificationsConfig notificationsConfig)
    {
        BoltProtocolV3.ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        return _boltProtocolV3.BeginTransactionAsync(
            connection,
            database,
            bookmarks,
            config,
            impersonatedUser,
            notificationsConfig);
    }

    public async Task<IResultCursor> RunInExplicitTransactionAsync(
        IConnection connection,
        Query query,
        bool reactive,
        long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);

        var streamBuilder = _protocolHandlerFactory.NewResultCursorBuilder(
            summaryBuilder,
            connection,
            RequestMore,
            CancelRequest,
            null,
            null,
            fetchSize,
            reactive);

        var runMessage = _protocolMessageFactory.NewRunWithMetadataMessage(connection, query);
        var runHandler = _protocolHandlerFactory.NewRunResponseHandler(streamBuilder, summaryBuilder);

        await connection.EnqueueAsync(runMessage, runHandler).ConfigureAwait(false);

        if (!reactive)
        {
            var pullMessage = _protocolMessageFactory.NewPullMessage(fetchSize);
            var pullHandler = _protocolHandlerFactory.NewPullResponseHandler(null, streamBuilder, summaryBuilder);
            await connection.EnqueueAsync(pullMessage, pullHandler).ConfigureAwait(false);
        }

        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
    {
        return _boltProtocolV3.CommitTransactionAsync(connection, bookmarksTracker);
    }

    public Task RollbackTransactionAsync(IConnection connection)
    {
        return _boltProtocolV3.RollbackTransactionAsync(connection);
    }

    private async Task LoginV51Async(
        IConnection connection,
        string userAgent,
        IAuthToken authToken,
        INotificationsConfig notificationsConfig)
    {
        var helloMessage = _protocolMessageFactory.NewHelloMessage(connection, userAgent, null, notificationsConfig);
        var helloHandler = _protocolHandlerFactory.NewHelloResponseHandler(connection);
        await connection.EnqueueAsync(helloMessage, helloHandler).ConfigureAwait(false);

        var logonMessage = _protocolMessageFactory.NewLogonMessage(connection, authToken);
        var logonHandler = _protocolHandlerFactory.NewHelloResponseHandler(connection);
        await connection.EnqueueAsync(logonMessage, logonHandler).ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<string, object>> GetRoutingTableWithQueryAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks)
    {
        connection.ConfigureMode(AccessMode.Read);

        var bookmarkTracker = new BookmarksTracker(bookmarks);
        var resourceHandler = new ConnectionResourceHandler(connection);
        var databaseParameter = string.IsNullOrWhiteSpace(database) ? null : database;
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

        var result = await RunInAutoCommitTransactionAsync(connection, autoCommitParams, null).ConfigureAwait(false);
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
        var dbParameter = string.IsNullOrWhiteSpace(database) ? null : database;
        //TODO: Consider refactoring logic of v43 into message factory.
        IRequestMessage message = connection.Version == BoltProtocolVersion.V4_3
            ? _protocolMessageFactory.NewRouteMessageV43(
                connection,
                bookmarks,
                dbParameter)
            : _protocolMessageFactory.NewRouteMessage(
                connection,
                bookmarks,
                dbParameter,
                impersonatedUser);

        var responseHandler = _protocolHandlerFactory.NewRouteResponseHandler();

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

    // Internal for tests.
    internal Func<IResultStreamBuilder, long, long, Task> RequestMore(
        IConnection connection,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id, n) =>
        {
            var pullMessage = _protocolMessageFactory.NewPullMessage(id, n);
            var pullResponseHandler = _protocolHandlerFactory.NewPullResponseHandler(
                bookmarksTracker,
                streamBuilder,
                summaryBuilder);

            await connection.EnqueueAsync(pullMessage, pullResponseHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        };
    }

    // Internal for tests.
    internal Func<IResultStreamBuilder, long, Task> CancelRequest(
        IConnection connection,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id) =>
        {
            var discardMessage = _protocolMessageFactory.NewDiscardMessage(id, ResultHandleMessage.All);
            var pullResponseHandler = _protocolHandlerFactory.NewPullResponseHandler(
                bookmarksTracker,
                streamBuilder,
                summaryBuilder);

            await connection.EnqueueAsync(discardMessage, pullResponseHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        };
    }
}
