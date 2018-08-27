// Copyright (c) 2002-2018 "Neo4j,"
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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
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

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger=null, bool byteArraySupportEnabled = true)
        {
            var messageFormat = BoltProtocolMessageFormat.V1;
            if (!byteArraySupportEnabled)
            {
                messageFormat = BoltProtocolMessageFormat.V1NoByteArray;
            }
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, messageFormat);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null, bool byteArraySupportEnabled=true)
        {
            var messageFormat = BoltProtocolMessageFormat.V1;
            if (!byteArraySupportEnabled)
            {
                messageFormat = BoltProtocolMessageFormat.V1NoByteArray;
            }
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, messageFormat);
        }

        public void Authenticate(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            connection.Enqueue(new InitMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            connection.Sync();
            ((ServerInfo)connection.Server).Version = serverVersionCollector.Server;

            if (!(ServerVersion.Version(serverVersionCollector.Server) >= ServerVersion.V3_2_0))
            {
                connection.ResetMessageReaderAndWriterForServerV3_1();
            }
        }

        public async Task AuthenticateAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            connection.Enqueue(new InitMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            await connection.SyncAsync().ConfigureAwait(false);
            ((ServerInfo)connection.Server).Version = serverVersionCollector.Server;
        }

        public IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark ignored1, TransactionConfig ignored2)
        {
            var resultBuilder = new ResultBuilder(statement.Text, statement.Parameters,
                connection.ReceiveOne, connection.Server, resultResourceHandler);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark bookmark, TransactionConfig txConfig)
        {
            var resultBuilder = new ResultCursorBuilder(statement.Text, statement.Parameters,
                connection.ReceiveOneAsync, connection.Server, resultResourceHandler);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);
            return await resultBuilder.PreBuildAsync().ConfigureAwait(false);
        }

        public void BeginTransaction(IConnection connection, Bookmark bookmark, TransactionConfig ignored)
        {
            IDictionary<string, object> parameters = bookmark?.AsBeginTransactionParameters();
            connection.Enqueue(new RunMessage(Begin, parameters), null, PullAll);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                connection.Sync();
            }
        }

        public async Task BeginTransactionAsync(IConnection connection, Bookmark bookmark, TransactionConfig ignored)
        {
            IDictionary<string, object> parameters = bookmark?.AsBeginTransactionParameters();
            connection.Enqueue(new RunMessage(Begin, parameters), null, PullAll);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public IStatementResult RunInExplicitTransaction(IConnection connection, Statement statement)
        {
            var resultBuilder = new ResultBuilder(statement.Text, statement.Parameters, connection.ReceiveOne,
                connection.Server);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement)
        {
            var resultBuilder = new ResultCursorBuilder(statement.Text, statement.Parameters, connection.ReceiveOneAsync,
                connection.Server);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);

            return await resultBuilder.PreBuildAsync().ConfigureAwait(false);
        }

        public Bookmark CommitTransaction(IConnection connection)
        {
            var bookmarkCollector = new BookmarkCollector();
            connection.Enqueue(Commit, bookmarkCollector, PullAll);
            connection.Sync();
            return bookmarkCollector.Bookmark;
        }

        public async Task<Bookmark> CommitTransactionAsync(IConnection connection)
        {
            var bookmarkCollector = new BookmarkCollector();
            connection.Enqueue(Commit, bookmarkCollector, PullAll);
            await connection.SyncAsync().ConfigureAwait(false);
            return bookmarkCollector.Bookmark;
        }

        public void RollbackTransaction(IConnection connection)
        {
            connection.Enqueue(Rollback, null, DiscardAll);
            connection.Sync();
        }

        public Task RollbackTransactionAsync(IConnection connection)
        {
            connection.Enqueue(Rollback, null, DiscardAll);
            return connection.SyncAsync();
        }

        public void Reset(IConnection connection)
        {
            connection.Enqueue(ResetMessage.Reset, null);
        }
    }
}
