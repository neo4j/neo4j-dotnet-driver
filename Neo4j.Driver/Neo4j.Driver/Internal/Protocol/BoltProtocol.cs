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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Messaging.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Protocol;

internal sealed class BoltProtocol : IBoltProtocol
{
    private readonly LegacyBoltProtocol _legacyProtocol;
    private readonly IRoutingTableProtocol _getRoutingTableProtocol;

    public BoltProtocol(IRoutingTableProtocol routingTableProtocol)
    {
        _getRoutingTableProtocol = routingTableProtocol;
        _legacyProtocol = new LegacyBoltProtocol();
    }

    public async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection, AutoCommitParams autoCommitParams)
    {
        var summaryBuilder = new SummaryBuilder(autoCommitParams.Query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(
            summaryBuilder,
            connection.ReceiveOneAsync,
            RequestMore(connection, summaryBuilder, autoCommitParams.BookmarksTracker),
            CancelRequest(connection, summaryBuilder,
                autoCommitParams.BookmarksTracker),
            autoCommitParams.ResultResourceHandler,
            autoCommitParams.FetchSize,
            autoCommitParams.Reactive);

        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);

        var pullMessage = default(PullMessage);
        var pullHandler = default(PullResponseHandler);
        if (!autoCommitParams.Reactive)
        {
            pullMessage = new PullMessage(autoCommitParams.FetchSize);
            pullHandler = new PullResponseHandler(streamBuilder, summaryBuilder, autoCommitParams.BookmarksTracker);
        }
        // Refactor to take AC Params
        var message = new RunWithMetadataMessage(
            connection.Version,
            autoCommitParams.Query,
            autoCommitParams.Bookmarks,
            autoCommitParams.Config,
            connection.GetEnforcedAccessMode(),
            autoCommitParams.Database,
            autoCommitParams.ImpersonatedUser);

        await connection.EnqueueAsync(message, runHandler, pullMessage, pullHandler)
            .ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);

        return streamBuilder.CreateCursor();
    }

    public Task BeginTransactionAsync(IConnection connection, string database, Bookmarks bookmarks, TransactionConfig config,
        string impersonatedUser)
    {
        return _legacyProtocol.BeginTransactionAsync(connection, database, bookmarks, config, impersonatedUser);
    }

    public async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection,
        Query query, bool reactive, long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync,
            RequestMore(connection, summaryBuilder, null),
            CancelRequest(connection, summaryBuilder, null), null,
            fetchSize, reactive);
        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);

        var pullMessage = default(PullMessage);
        var pullHandler = default(PullResponseHandler);
        if (!reactive)
        {
            pullMessage = new PullMessage(fetchSize);
            pullHandler = new PullResponseHandler(streamBuilder, summaryBuilder, null);
        }
        
        await connection.EnqueueAsync(new RunWithMetadataMessage(connection.Version, query),
                runHandler, pullMessage, pullHandler)
            .ConfigureAwait(false);
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

    private static Func<IResultStreamBuilder, long, long, Task> RequestMore(IConnection connection,
        SummaryBuilder summaryBuilder, IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id, n) =>
        {
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            await connection
                .EnqueueAsync(new PullMessage(id, n), pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        };
    }

    private static Func<IResultStreamBuilder, long, Task> CancelRequest(IConnection connection,
        SummaryBuilder summaryBuilder, IBookmarksTracker bookmarksTracker)
    {
        return async (streamBuilder, id) =>
        {
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            await connection
                .EnqueueAsync(new DiscardMessage(id, All), pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        };
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

    public Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database,
        string impersonatedUser, Bookmarks bookmarks)
    {
        return connection.Version >= BoltProtocolVersion.V4_3
            ? _getRoutingTableProtocol.GetRoutingTable(connection, database, impersonatedUser, bookmarks)
            : _legacyProtocol.GetRoutingTable(connection, database, impersonatedUser, bookmarks);
    }
}