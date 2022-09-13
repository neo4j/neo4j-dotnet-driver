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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Messaging.V4;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Protocol;

internal class BoltProtocolV4_0 : BoltProtocolV3
{
    public override async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
        Query query, bool reactive, IBookmarksTracker bookmarksTracker, IResultResourceHandler resultResourceHandler,
        string database, Bookmarks bookmarks, TransactionConfig config, string impersonatedUser, long fetchSize = Config.Infinite)
    {
        var summaryBuilder = new SummaryBuilder(query, connection.Server);
        var streamBuilder = new ResultCursorBuilder(
            summaryBuilder, 
            connection.ReceiveOneAsync,
            RequestMore(connection, summaryBuilder, bookmarksTracker),
            CancelRequest(connection, summaryBuilder, bookmarksTracker),
            resultResourceHandler,
            fetchSize, reactive);
        
        var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);

        var pullMessage = default(PullMessage);
        var pullHandler = default(PullResponseHandler);
        if (!reactive)
        {
            pullMessage = new PullMessage(fetchSize);
            pullHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
        }

        var message = new RunWithMetadataMessage(connection, query, bookmarks, config,
            connection.GetEnforcedAccessMode(), database, impersonatedUser);

        await connection.EnqueueAsync(message, runHandler, pullMessage, pullHandler)
            .ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);

        return streamBuilder.CreateCursor();
    }

    public override async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection,
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

        await connection.EnqueueAsync(new RunWithMetadataMessage(connection, query),
                runHandler, pullMessage, pullHandler)
            .ConfigureAwait(false);
        await connection.SendAsync().ConfigureAwait(false);
        return streamBuilder.CreateCursor();
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
}