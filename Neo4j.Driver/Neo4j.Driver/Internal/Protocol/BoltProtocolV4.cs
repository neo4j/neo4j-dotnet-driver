// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Messaging.V4;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;
using V3 = Neo4j.Driver.Internal.MessageHandling.V3;
using V4 = Neo4j.Driver.Internal.MessageHandling.V4;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4 : BoltProtocolV3
    {
        public static readonly BoltProtocolV4 BoltV4 = new BoltProtocolV4();

        public override IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V4);
        }

        public override IMessageReader NewReader(Stream stream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V4);
        }

        public override async Task BeginTransactionAsync(IConnection connection, string database, Bookmark bookmark,
            TransactionOptions optionsBuilder)
        {
            await connection.EnqueueAsync(
                    new BeginMessage(database, bookmark, optionsBuilder?.Timeout, optionsBuilder?.Metadata,
                        connection.GetEnforcedAccessMode()),
                    new V3.BeginResponseHandler())
                .ConfigureAwait(false);
            if (bookmark != null && bookmark.Values.Any())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public override async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement, bool reactive, IBookmarkTracker bookmarkTracker,
            IResultResourceHandler resultResourceHandler,
            string database, Bookmark bookmark, TransactionOptions optionsBuilder, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(statement, connection.Server);
            var streamBuilder = new StatementResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync,
                RequestMore(connection, summaryBuilder, bookmarkTracker),
                CancelRequest(connection, summaryBuilder, bookmarkTracker),
                resultResourceHandler,
                fetchSize, reactive);
            var runHandler = new V4.RunResponseHandler(streamBuilder, summaryBuilder);

            var pullMessage = default(PullMessage);
            var pullHandler = default(V4.PullResponseHandler);
            if (!reactive)
            {
                pullMessage = new PullMessage(fetchSize);
                pullHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarkTracker);
            }

            await connection
                .EnqueueAsync(
                    new RunWithMetadataMessage(statement, database, bookmark, optionsBuilder,
                        connection.GetEnforcedAccessMode()), runHandler,
                    pullMessage, pullHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public override async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement, bool reactive, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(statement, connection.Server);
            var streamBuilder = new StatementResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync,
                RequestMore(connection, summaryBuilder, null),
                CancelRequest(connection, summaryBuilder, null), null,
                fetchSize, reactive);
            var runHandler = new V4.RunResponseHandler(streamBuilder, summaryBuilder);

            var pullMessage = default(PullMessage);
            var pullHandler = default(V4.PullResponseHandler);
            if (!reactive)
            {
                pullMessage = new PullMessage(fetchSize);
                pullHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, null);
            }

            await connection.EnqueueAsync(new RunWithMetadataMessage(statement),
                    runHandler, pullMessage, pullHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        private static Func<StatementResultCursorBuilder, long, long, Task> RequestMore(IConnection connection,
            SummaryBuilder summaryBuilder, IBookmarkTracker bookmarkTracker)
        {
            return async (streamBuilder, id, n) =>
            {
                var pullAllHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarkTracker);
                await connection
                    .EnqueueAsync(new PullMessage(id, n), pullAllHandler)
                    .ConfigureAwait(false);
                await connection.SendAsync().ConfigureAwait(false);
            };
        }

        private static Func<StatementResultCursorBuilder, long, Task> CancelRequest(IConnection connection,
            SummaryBuilder summaryBuilder, IBookmarkTracker bookmarkTracker)
        {
            return async (streamBuilder, id) =>
            {
                var pullAllHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarkTracker);
                await connection
                    .EnqueueAsync(new DiscardMessage(id, All), pullAllHandler)
                    .ConfigureAwait(false);
                await connection.SendAsync().ConfigureAwait(false);
            };
        }
    }
}