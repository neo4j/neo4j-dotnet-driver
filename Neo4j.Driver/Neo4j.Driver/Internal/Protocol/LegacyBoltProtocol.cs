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
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal;

internal sealed class LegacyBoltProtocol : IBoltProtocol
{
    public static readonly LegacyBoltProtocol Instance = new();

    private LegacyBoltProtocol()
    {
    }
    
    public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
    {
        await connection.EnqueueAsync(
                new HelloMessage(
                    connection.Version,
                    userAgent,
                    authToken.AsDictionary(),
                    connection.RoutingContext),
                new HelloResponseHandler(connection))
            .ConfigureAwait(false);

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
        ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        connection = connection ??
            throw new ProtocolException("Attempting to get a routing table on a null connection");

        //TODO: Proper message
        bookmarks = connection.Version.MajorVersion > 3
            ? bookmarks
            : bookmarks == null
                ? null
                : throw new Exception("Server does not support bookmarks");

        var bookmarkTracker = new BookmarksTracker(bookmarks);
        var resourceHandler = new ConnectionResourceHandler(connection);
        var sessionDb = connection.SupportsMultiDatabase() ? "system" : null;

        connection.Configure(null, AccessMode.Read);

        var query = GetRoutingTableQuery(connection, database);

        var autoCommitParams = new AutoCommitParams
        {
            Query = query,
            BookmarksTracker = bookmarkTracker,
            ResultResourceHandler = resourceHandler,
            Database = sessionDb,
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

    public async Task<IResultCursor> RunInAutoCommitTransactionAsync(
        IConnection connection,
        AutoCommitParams autoCommitParams)
    {
        ValidateImpersonatedUserForVersion(connection, autoCommitParams.ImpersonatedUser);
        ValidateDatabase(connection, autoCommitParams.Database);

        var summaryBuilder = new SummaryBuilder(autoCommitParams.Query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(
            summaryBuilder,
            connection.ReceiveOneAsync,
            null,
            null,
            autoCommitParams.ResultResourceHandler);

        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
        var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, autoCommitParams.BookmarksTracker);

        var autoCommitMessage = new RunWithMetadataMessage(
            connection.Version,
            autoCommitParams.Query,
            autoCommitParams.Bookmarks,
            autoCommitParams.Config,
            connection.Mode ?? throw new InvalidOperationException("Connection should have its Mode property set."),
            null,
            autoCommitParams.ImpersonatedUser);

        await connection.EnqueueAsync(autoCommitMessage, runHandler, PullAllMessage.Instance, pullAllHandler)
            .ConfigureAwait(false);

        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public async Task BeginTransactionAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser)
    {
        ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        ValidateDatabase(connection, database);
        
        var mode = connection.Mode ??
            throw new InvalidOperationException("Connection should have its Mode property set.");
        
        await connection.EnqueueAsync(
                new BeginMessage(
                    connection.Version,
                    database,
                    bookmarks,
                    config,
                    mode,
                    impersonatedUser),
                NoOpResponseHandler.Instance)
            .ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
    }

    public async Task<IResultCursor> RunInExplicitTransactionAsync(
        IConnection connection,
        Query query,
        bool reactive,
        long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, null);

        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
        var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, null);

        var message = new RunWithMetadataMessage(connection.Version, query);

        await connection.EnqueueAsync(message, runHandler, PullAllMessage.Instance, pullAllHandler)
            .ConfigureAwait(false);

        await connection.SendAsync().ConfigureAwait(false);

        return streamBuilder.CreateCursor();
    }

    public async Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
    {
        await connection.EnqueueAsync(CommitMessage.Instance, new CommitResponseHandler(bookmarksTracker))
            .ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
    }

    public async Task RollbackTransactionAsync(IConnection connection)
    {
        await connection.EnqueueAsync(RollbackMessage.Instance, NoOpResponseHandler.Instance)
            .ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
    }

    private static void ValidateDatabase(IConnection connection, string database)
    {
        if (connection.Version.MajorVersion >= 4)
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

    private static Query GetRoutingTableQuery(IConnection connection, string database)
    {
        var procedure = connection.Version.MajorVersion == 3
            ? "CALL dbms.cluster.routing.getRoutingTable($context)"
            : "CALL dbms.routing.getRoutingTable($context, $database)";

        var parameters = new Dictionary<string, object>
        {
            ["context"] = connection.RoutingContext
        };

        if (connection.Version.MajorVersion > 3)
        {
            parameters.Add(database, string.IsNullOrWhiteSpace(database) ? null : database);
        }

        return new Query(procedure, parameters);
    }

    private static void ValidateImpersonatedUserForVersion(IConnection conn, string impersonatedUser)
    {
        if (conn.Version >= BoltProtocolVersion.V4_4)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(impersonatedUser))
        {
            throw new ArgumentException(
                "Bolt Protocol 3.0 does not support impersonatedUser, " +
                "yet has been passed a non null impersonated user string");
        }
    }
}
