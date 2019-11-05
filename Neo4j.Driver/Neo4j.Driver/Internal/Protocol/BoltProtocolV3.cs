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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using V3 = Neo4j.Driver.Internal.MessageHandling.V3;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3 : IBoltProtocol
    {
        public static readonly BoltProtocolV3 BoltV3 = new BoltProtocolV3();

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V3);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V3);
        }

        public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection
                .EnqueueAsync(new HelloMessage(userAgent, authToken.AsDictionary()),
                    new V3.HelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public virtual async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement, bool reactive, IBookmarkTracker bookmarkTracker,
            IResultResourceHandler resultResourceHandler,
            string database, Bookmark bookmark, TransactionOptions optionsBuilder, long fetchSize = Config.Infinite)
        {
            AssertNullDatabase(database);

            var summaryBuilder = new SummaryBuilder(statement, connection.Server);
            var streamBuilder = new StatementResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null,
                resultResourceHandler);
            var runHandler = new V3.RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new V3.PullResponseHandler(streamBuilder, summaryBuilder, bookmarkTracker);
            await connection
                .EnqueueAsync(
                    new RunWithMetadataMessage(statement, bookmark, optionsBuilder, connection.GetEnforcedAccessMode()),
                    runHandler,
                    PullAll, pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public virtual async Task BeginTransactionAsync(IConnection connection, string database, Bookmark bookmark,
            TransactionOptions optionsBuilder)
        {
            AssertNullDatabase(database);

            await connection.EnqueueAsync(
                    new BeginMessage(bookmark, optionsBuilder, connection.GetEnforcedAccessMode()),
                    new V3.BeginResponseHandler())
                .ConfigureAwait(false);
            if (bookmark != null && bookmark.Values.Any())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public virtual async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement, bool reactive, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(statement, connection.Server);
            var streamBuilder =
                new StatementResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, null);
            var runHandler = new V3.RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new V3.PullResponseHandler(streamBuilder, summaryBuilder, null);
            await connection.EnqueueAsync(new RunWithMetadataMessage(statement),
                    runHandler, PullAll, pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public async Task CommitTransactionAsync(IConnection connection, IBookmarkTracker bookmarkTracker)
        {
            await connection.EnqueueAsync(CommitMessage.Commit, new V3.CommitResponseHandler(bookmarkTracker))
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public async Task RollbackTransactionAsync(IConnection connection)
        {
            await connection.EnqueueAsync(RollbackMessage.Rollback, new V3.RollbackResponseHandler())
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public Task ResetAsync(IConnection connection)
        {
            return connection.EnqueueAsync(ResetMessage.Reset, new V3.ResetResponseHandler());
        }

        public async Task LogoutAsync(IConnection connection)
        {
            await connection.EnqueueAsync(GoodbyeMessage.Goodbye, new NoOpResponseHandler()).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        }

        private void AssertNullDatabase(string database)
        {
            if (database != null)
            {
                throw new ClientException(
                    "Driver is connected to a server that does not support multiple databases. " +
                    "Please upgrade to neo4j 4.0.0 or later in order to use this functionality");
            }
        }
    }
}