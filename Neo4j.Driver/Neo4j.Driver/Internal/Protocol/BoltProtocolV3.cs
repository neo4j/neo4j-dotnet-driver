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

internal sealed class BoltProtocolV3 : IBoltProtocol
{
    internal static readonly BoltProtocolV3 Instance = new();
    private readonly IBoltProtocolHandlerFactory _protocolHandlerFactory;
    private readonly IBoltProtocolMessageFactory _protocolMessageFactory;

    internal BoltProtocolV3(
        IBoltProtocolMessageFactory protocolMessageFactory = null,
        IBoltProtocolHandlerFactory protocolHandlerFactory = null)
    {
        _protocolMessageFactory = protocolMessageFactory ?? BoltProtocolMessageFactory.Instance;
        _protocolHandlerFactory = protocolHandlerFactory ?? BoltProtocolHandlerFactory.Instance;
    }

    public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken, INotificationsConfig notificationsConfig)
    {
        var message = _protocolMessageFactory.NewHelloMessage(connection, userAgent, authToken, notificationsConfig);
        var handler = _protocolHandlerFactory.NewHelloResponseHandler(connection);

        await connection.EnqueueAsync(message, handler).ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    public async Task LogoutAsync(IConnection connection)
    {
        await connection.EnqueueAsync(GoodbyeMessage.Instance, NoOpResponseHandler.Instance).ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
    }

    public Task ResetAsync(IConnection connection)
    {
        return connection.EnqueueAsync(ResetMessage.Instance, NoOpResponseHandler.Instance);
    }

    public async Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        IConnection connection,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        connection = connection ??
            throw new ProtocolException("Attempting to get a routing table on a null connection");

        ValidateImpersonatedUserForVersion(connection, impersonatedUser);

        connection.ConfigureMode(AccessMode.Read);

        var bookmarkTracker = new BookmarksTracker(bookmarks);
        var resourceHandler = new ConnectionResourceHandler(connection);

        var autoCommitParams = new AutoCommitParams
        {
            Query = new Query(
                "CALL dbms.cluster.routing.getRoutingTable($context)",
                new Dictionary<string, object>
                {
                    ["context"] = connection.RoutingContext
                }),
            BookmarksTracker = bookmarkTracker,
            ResultResourceHandler = resourceHandler
        };

        var result = await RunInAutoCommitTransactionAsync(connection, autoCommitParams, null).ConfigureAwait(false);
        var record = await result.SingleAsync().ConfigureAwait(false);

        //Since 4.4 the Routing information will contain a db.
        //Earlier versions need to populate this here as it's not received in the older route response...
        var finalDictionary = record.Values.ToDictionary();
        finalDictionary["db"] = database;

        return (IReadOnlyDictionary<string, object>)finalDictionary;
    }

    public async Task<IResultCursor> RunInAutoCommitTransactionAsync(
        IConnection connection,
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig)
    {
        ValidateImpersonatedUserForVersion(connection, autoCommitParams.ImpersonatedUser);
        ValidateDatabase(connection, autoCommitParams.Database);

        var summaryBuilder = new SummaryBuilder(autoCommitParams.Query, connection.Server);
        var streamBuilder = _protocolHandlerFactory.NewResultCursorBuilder(
            summaryBuilder,
            connection,
            null,
            null,
            null,
            autoCommitParams.ResultResourceHandler,
            Config.Infinite,
            false);

        var runHandler = _protocolHandlerFactory.NewRunResponseHandlerV3(streamBuilder, summaryBuilder);
        var pullAllHandler = _protocolHandlerFactory.NewPullAllResponseHandler(
            streamBuilder,
            summaryBuilder,
            autoCommitParams.BookmarksTracker);

        var autoCommitMessage = _protocolMessageFactory.NewRunWithMetadataMessage(connection, autoCommitParams);

        await connection.EnqueueAsync(autoCommitMessage, runHandler).ConfigureAwait(false);
        await connection.EnqueueAsync(PullAllMessage.Instance, pullAllHandler).ConfigureAwait(false);

        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public async Task BeginTransactionAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser,
        INotificationsConfig notificationsConfig)
    {
        ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        ValidateDatabase(connection, database);

        var mode = connection.Mode ??
            throw new InvalidOperationException("Connection should have its Mode property set.");

        var message = _protocolMessageFactory.NewBeginMessage(
            connection,
            database,
            bookmarks,
            config,
            mode,
            impersonatedUser);

        await connection.EnqueueAsync(message, NoOpResponseHandler.Instance).ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
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
            null,
            null,
            null,
            null,
            Config.Infinite,
            false);

        var runHandler = _protocolHandlerFactory.NewRunResponseHandlerV3(streamBuilder, summaryBuilder);
        var pullAllHandler = _protocolHandlerFactory.NewPullAllResponseHandler(streamBuilder, summaryBuilder, null);

        var message = _protocolMessageFactory.NewRunWithMetadataMessage(connection, query);

        await connection.EnqueueAsync(message, runHandler).ConfigureAwait(false);
        await connection.EnqueueAsync(PullAllMessage.Instance, pullAllHandler).ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);

        return streamBuilder.CreateCursor();
    }

    public async Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
    {
        var handler = _protocolHandlerFactory.NewCommitResponseHandler(bookmarksTracker);

        await connection.EnqueueAsync(CommitMessage.Instance, handler).ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    public async Task RollbackTransactionAsync(IConnection connection)
    {
        await connection.EnqueueAsync(RollbackMessage.Instance, NoOpResponseHandler.Instance).ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    internal static void ValidateDatabase(IConnection connection, string database)
    {
        if (connection.Version >= BoltProtocolVersion.V4_0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(database))
        {
            throw new ClientException(
                "Driver is connected to a server that does not support multiple databases. " +
                "Please upgrade to neo4j 4.0.0 or later in order to use this functionality");
        }
    }

    internal static void ValidateImpersonatedUserForVersion(IConnection conn, string impersonatedUser)
    {
        if (conn.Version >= BoltProtocolVersion.V4_4)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(impersonatedUser))
        {
            throw new ArgumentException(
                $"Bolt Protocol {conn.Version} does not support impersonatedUser, " +
                "but has been passed a non-null impersonated user string");
        }
    }
}
