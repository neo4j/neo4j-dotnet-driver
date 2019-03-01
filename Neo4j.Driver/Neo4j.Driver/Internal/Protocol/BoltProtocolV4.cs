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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Messaging.V4;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4 : IBoltProtocol
    {
        public static readonly BoltProtocolV4 BoltV4 = new BoltProtocolV4();

        public IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V4);
        }

        public IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, IDriverLogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V4);
        }

        public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var collector = new HelloMessageResponseCollector();
            await connection.EnqueueAsync(new HelloMessage(userAgent, authToken.AsDictionary()), collector)
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
            ((ServerInfo) connection.Server).Version = collector.Server;
            connection.UpdateId(collector.ConnectionId);
        }

        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark bookmark, TransactionConfig txConfig)
        {
            var resultBuilder = new ResultCursorBuilder(NewSummaryCollector(statement, connection.Server),
                connection.ReceiveOneAsync, resultResourceHandler);
            await connection.EnqueueAsync(new RunWithMetadataMessage(statement, bookmark, txConfig, connection.GetEnforcedAccessMode()), resultBuilder,
                new PullNMessage(All)).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return resultBuilder.PreBuild();
        }

        public async Task BeginTransactionAsync(IConnection connection, Bookmark bookmark, TransactionConfig txConfig)
        {
            await connection.EnqueueAsync(new BeginMessage(bookmark, txConfig, connection.GetEnforcedAccessMode()), null).ConfigureAwait(false);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement)
        {
            var resultBuilder = new ResultCursorBuilder(
                NewSummaryCollector(statement, connection.Server), connection.ReceiveOneAsync);
            await connection.EnqueueAsync(new RunWithMetadataMessage(statement, connection.GetEnforcedAccessMode()), resultBuilder, new PullNMessage(All))
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);

            return resultBuilder.PreBuild();
        }

        public async Task<Bookmark> CommitTransactionAsync(IConnection connection)
        {
            var bookmarkCollector = new BookmarkCollector();
            await connection.EnqueueAsync(CommitMessage.Commit, bookmarkCollector).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
            return bookmarkCollector.Bookmark;
        }

        public async Task RollbackTransactionAsync(IConnection connection)
        {
            await connection.EnqueueAsync(RollbackMessage.Rollback, null).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public Task ResetAsync(IConnection connection)
        {
            return connection.EnqueueAsync(ResetMessage.Reset, null);
        }

        public async Task LogoutAsync(IConnection connection)
        {
            await connection.EnqueueAsync(GoodbyeMessage.Goodbye, null);
            await connection.SendAsync();
        }

        private SummaryCollector NewSummaryCollector(Statement statement, IServerInfo serverInfo)
        {
            return new SummaryCollectorV3(statement, serverInfo);
        }
    }
}