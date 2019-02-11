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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver;
using static Neo4j.Driver.Internal.Messaging.DiscardAllMessage;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV1 : IBoltProtocol
    {
        public static readonly BoltProtocolV1 BoltV1 = new BoltProtocolV1();

        private const string Begin = "BEGIN";
        public static readonly IRequestMessage Commit = new RunMessage("COMMIT");
        public static readonly IRequestMessage Rollback = new RunMessage("ROLLBACK");

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V1);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings,
            IDriverLogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V1);
        }

        public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            await connection.EnqueueAsync(new InitMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            await connection.SyncAsync().ConfigureAwait(false);
            ((ServerInfo) connection.Server).Version = serverVersionCollector.Server;
        }

        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement, IResultResourceHandler resultResourceHandler, Bookmark ignored,
            TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            var resultBuilder = new ResultCursorBuilder(NewSummaryCollector(statement, connection.Server),
                connection.ReceiveOneAsync, resultResourceHandler);
            await connection.EnqueueAsync(new RunMessage(statement), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);
            return resultBuilder.PreBuild();
        }

        public async Task BeginTransactionAsync(IConnection connection, Bookmark bookmark, TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            IDictionary<string, object> parameters = bookmark?.AsBeginTransactionParameters();
            await connection.EnqueueAsync(new RunMessage(Begin, parameters), null, PullAll);
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
            await connection.EnqueueAsync(new RunMessage(statement), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);

            return resultBuilder.PreBuild();
        }

        public async Task<Bookmark> CommitTransactionAsync(IConnection connection)
        {
            var bookmarkCollector = new BookmarkCollector();
            await connection.EnqueueAsync(Commit, bookmarkCollector, PullAll);
            await connection.SyncAsync().ConfigureAwait(false);
            return bookmarkCollector.Bookmark;
        }

        public async Task RollbackTransactionAsync(IConnection connection)
        {
            await connection.EnqueueAsync(Rollback, null, DiscardAll).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public Task ResetAsync(IConnection connection)
        {
            return connection.EnqueueAsync(ResetMessage.Reset, null);
        }

        public Task LogoutAsync(IConnection connection)
        {
            return TaskHelper.GetCompletedTask();
        }

        private void AssertNullOrEmptyTransactionConfig(TransactionConfig txConfig)
        {
            if (txConfig != null && !txConfig.IsEmpty())
            {
                throw new ArgumentException(
                    "Driver is connected to the database that does not support transaction configuration. " +
                    "Please upgrade to neo4j 3.5.0 or later in order to use this functionality");
            }
        }

        private static SummaryCollector NewSummaryCollector(Statement statement, IServerInfo serverInfo)
        {
            return new SummaryCollector(statement, serverInfo);
        }
    }
}