﻿// Copyright (c) "Neo4j"
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

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, IDriverLogger logger=null, bool byteArraySupportEnabled = true)
        {
            var messageFormat = BoltProtocolMessageFormat.V1;
            if (!byteArraySupportEnabled)
            {
                messageFormat = BoltProtocolMessageFormat.V1NoByteArray;
            }
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, messageFormat);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, IDriverLogger logger = null, bool byteArraySupportEnabled=true)
        {
            var messageFormat = BoltProtocolMessageFormat.V1;
            if (!byteArraySupportEnabled)
            {
                messageFormat = BoltProtocolMessageFormat.V1NoByteArray;
            }
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, messageFormat);
        }

        public void Login(IConnection connection, string userAgent, IAuthToken authToken)
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

        public async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            connection.Enqueue(new InitMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            await connection.SyncAsync().ConfigureAwait(false);
            ((ServerInfo)connection.Server).Version = serverVersionCollector.Server;
        }

        public IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark ignored, TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            var resultBuilder = new ResultBuilder(NewSummaryCollector(statement, connection.Server),
                connection.ReceiveOne, resultResourceHandler);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
            Statement statement, IResultResourceHandler resultResourceHandler, Bookmark ignored, TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            var resultBuilder = new ResultCursorBuilder(NewSummaryCollector(statement, connection.Server),
                connection.ReceiveOneAsync, resultResourceHandler);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);
            return await resultBuilder.PreBuildAsync().ConfigureAwait(false);
        }

        public void BeginTransaction(IConnection connection, Bookmark bookmark, TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            IDictionary<string, object> parameters = bookmark?.AsBeginTransactionParameters();
            connection.Enqueue(new RunMessage(Begin, parameters), null, PullAll);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                connection.Sync();
            }
        }

        public async Task BeginTransactionAsync(IConnection connection, Bookmark bookmark, TransactionConfig txConfig)
        {
            AssertNullOrEmptyTransactionConfig(txConfig);
            IDictionary<string, object> parameters = bookmark?.AsBeginTransactionParameters();
            connection.Enqueue(new RunMessage(Begin, parameters), null, PullAll);
            if (bookmark != null && !bookmark.IsEmpty())
            {
                await connection.SyncAsync().ConfigureAwait(false);
            }
        }

        public IStatementResult RunInExplicitTransaction(IConnection connection, Statement statement)
        {
            var resultBuilder = new ResultBuilder(
                NewSummaryCollector(statement, connection.Server),connection.ReceiveOne);
            connection.Enqueue(new RunMessage(statement), resultBuilder, PullAll);
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Statement statement)
        {
            var resultBuilder = new ResultCursorBuilder(
                NewSummaryCollector(statement, connection.Server), connection.ReceiveOneAsync);
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

        public void Logout(IConnection connection)
        {
        }

        public Task LogoutAsync(IConnection connection)
        {
            return TaskHelper.GetCompletedTask();
        }

        private void AssertNullOrEmptyTransactionConfig(TransactionConfig txConfig)
        {
            if ( txConfig != null && !txConfig.IsEmpty() )
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
