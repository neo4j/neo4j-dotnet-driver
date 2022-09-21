// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;

namespace Neo4j.Driver.Internal.Protocol;

internal class BoltProtocolV3 : IBoltProtocol
{
    protected const string RoutingTableDbKey = "db";

    public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
    {
        await connection.EnqueueAsync(
                new HelloMessage(connection.Version, userAgent, authToken.AsDictionary(), connection.RoutingContext),
                new HelloResponseHandler(connection))
            .ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    public virtual async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
        Query query, bool reactive, IBookmarksTracker bookmarksTracker, IResultResourceHandler resultResourceHandler,
        string database, Bookmarks bookmarks, TransactionConfig config, string impersonatedUser,
        long fetchSize = Config.Infinite)
    {
        ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        ValidateDatabase(connection, database);

        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null,
            resultResourceHandler);

        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
        var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);

        await connection
            .EnqueueAsync(
                new RunWithMetadataMessage(connection, query, bookmarks, config, connection.GetEnforcedAccessMode(),
                    null,
                    impersonatedUser), runHandler, PullAll, pullAllHandler).ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public async Task BeginTransactionAsync(IConnection connection, string database, Bookmarks bookmarks,
        TransactionConfig config, string impersonatedUser)
    {
        ValidateImpersonatedUserForVersion(connection, impersonatedUser);
        ValidateDatabase(connection, database);

        await connection.EnqueueAsync(
                new BeginMessage(connection, database, bookmarks, config, connection.GetEnforcedAccessMode(),
                    impersonatedUser), NoOpResponseHandler.Instance)
            .ConfigureAwait(false);

        await connection.SyncAsync().ConfigureAwait(false);
    }

    public virtual async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection, Query query,
        bool reactive, long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, null);
        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
        var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, null);

        await connection.EnqueueAsync(new RunWithMetadataMessage(connection, query),
                runHandler, PullAll, pullAllHandler)
            .ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
    }

    public async Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
    {
        await connection.EnqueueAsync(CommitMessage.Commit, new CommitResponseHandler(bookmarksTracker))
            .ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    public async Task RollbackTransactionAsync(IConnection connection)
    {
        await connection.EnqueueAsync(RollbackMessage.Rollback, NoOpResponseHandler.Instance)
            .ConfigureAwait(false);
        await connection.SyncAsync().ConfigureAwait(false);
    }

    public Task ResetAsync(IConnection connection)
    {
        return connection.EnqueueAsync(ResetMessage.Reset, NoOpResponseHandler.Instance);
    }

    public async Task LogoutAsync(IConnection connection)
    {
        await connection.EnqueueAsync(GoodbyeMessage.Goodbye, NoOpResponseHandler.Instance).ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection,
        string database, string impersonatedUser, Bookmarks bookmarks)
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
        var sessionDb = connection.SupportsMultidatabase() ? "system" : null;
        
        connection.Configure(sessionDb, AccessMode.Read);
        
        GetProcedureAndParameters(connection, database, out var procedure, out var parameters);
        var query = new Query(procedure, parameters);

        var result = await RunInAutoCommitTransactionAsync(connection, query, false, bookmarkTracker, resourceHandler,
            sessionDb, bookmarks, null, null).ConfigureAwait(false);
        var record = await result.SingleAsync().ConfigureAwait(false);

        //Since 4.4 the Routing information will contain a db.
        //Earlier versions need to populate this here as it's not received in the older route response...
        var finalDictionary = record.Values.ToDictionary();
        finalDictionary[RoutingTableDbKey] = database;

        return (IReadOnlyDictionary<string, object>) finalDictionary;
    }

    private static void ValidateDatabase(IConnection connection, string database)
    {
        if (connection.Version.MajorVersion == 3 && !string.IsNullOrEmpty(database))
            throw new ClientException(
                "Driver is connected to a server that does not support multiple databases. Please upgrade to neo4j 4.0.0 or later in order to use this functionality");
    }

    public static void GetProcedureAndParameters(IConnection connection, string database,
        out string procedure, out Dictionary<string, object> parameters)
    {
        procedure = RoutingTableProcedureName(connection);
        parameters = new Dictionary<string, object>
        {
            ["context"] = connection.RoutingContext
        };

        if (connection.Version.MajorVersion > 3)
            parameters.Add(database, string.IsNullOrWhiteSpace(database) ? null : database);
    }

    protected static void ValidateImpersonatedUserForVersion(IConnection connection, string impersonatedUser)
    {
        if (connection.Version >= BoltProtocolVersion.V4_4)
            return;

        if (impersonatedUser is not null)
            throw new ArgumentException(
                $"Bolt Protocol {connection.Version} does not support impersonatedUser, yet has been passed a non null impersonated user string");
    }

    protected static string RoutingTableProcedureName(IConnection connection)
    {
        return connection.Version.MajorVersion == 3
            ? "CALL dbms.cluster.routing.getRoutingTable($context)"
            : "CALL dbms.routing.getRoutingTable($context, $database)";
    }

    protected class ConnectionResourceHandler : IResultResourceHandler
    {
        public ConnectionResourceHandler(IConnection conn)
        {
            Connection = conn;
        }

        private IConnection Connection { get; }

        public Task OnResultConsumedAsync()
        {
            return CloseConnection();
        }

        private Task CloseConnection()
        {
            return Connection.CloseAsync();
        }
    }

    protected class BookmarksTracker : IBookmarksTracker
    {
        public BookmarksTracker(Bookmarks bookmarks)
        {
            InternalBookmarks = bookmarks;
        }

        private Bookmarks InternalBookmarks { get; set; }

        public void UpdateBookmarks(Bookmarks bookmarks, IDatabaseInfo dbInfo = null)
        {
            if (InternalBookmarks != null && InternalBookmarks.Values.Any())
                InternalBookmarks = bookmarks;
        }
    }
}