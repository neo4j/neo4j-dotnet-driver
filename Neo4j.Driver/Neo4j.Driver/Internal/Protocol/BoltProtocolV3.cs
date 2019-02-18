// Copyright (c) 2002-2019 Neo4j Sweden AB [http://neo4j.com]
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

using System.IO;
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
using V1 = Neo4j.Driver.Internal.MessageHandling.V1;
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
            Statement statement, IBookmarkTracker bookmarkTracker, IResultResourceHandler resultResourceHandler,
            Bookmark bookmark, TransactionConfig txConfig)
        {
            var streamBuilder = new ResultStreamBuilder(statement, connection.Server,
                connection.ReceiveOneAsync, null, null, CancellationToken.None, resultResourceHandler);
            var runHandler = new V3.RunResponseHandler(streamBuilder);
            var pullAllHandler = new V3.PullResponseHandler(streamBuilder, bookmarkTracker);
            await connection
                .EnqueueAsync(new RunWithMetadataMessage(statement, bookmark, txConfig, connection.GetEnforcedAccessMode()), runHandler, PullAll,
                    pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public async Task BeginTransactionAsync(IConnection connection, Bookmark bookmark, TransactionConfig txConfig)
        {
            await connection.EnqueueAsync(new BeginMessage(bookmark, txConfig, connection.GetEnforcedAccessMode()), new V1.BeginResponseHandler())
                .ConfigureAwait(false);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public virtual async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement)
        {
            var streamBuilder = new ResultStreamBuilder(statement, connection.Server,
                connection.ReceiveOneAsync, null, null, CancellationToken.None, null);
            var runHandler = new V3.RunResponseHandler(streamBuilder);
            var pullAllHandler = new V3.PullResponseHandler(streamBuilder, null);
            await connection.EnqueueAsync(new RunWithMetadataMessage(statement, connection.GetEnforcedAccessMode()), runHandler, PullAll, pullAllHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public async Task CommitTransactionAsync(IConnection connection, IBookmarkTracker bookmarkTracker)
        {
            await connection.EnqueueAsync(CommitMessage.Commit, new V1.CommitResponseHandler(bookmarkTracker))
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public async Task RollbackTransactionAsync(IConnection connection)
        {
            await connection.EnqueueAsync(RollbackMessage.Rollback, new V1.RollbackResponseHandler())
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public Task ResetAsync(IConnection connection)
        {
            return connection.EnqueueAsync(ResetMessage.Reset, new V1.ResetResponseHandler());
        }

        public async Task LogoutAsync(IConnection connection)
        {
            await connection.EnqueueAsync(GoodbyeMessage.Goodbye, null);
            await connection.SendAsync();
        }
    }
}