// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3 : IBoltProtocol
    {
        public static readonly BoltProtocolV3 BoltV3 = new BoltProtocolV3();
        
        public IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            ILogger logger = null, bool ignored = true)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V3);
        }

        public IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null,
            bool ignored = true)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V3);
        }

        public void Authenticate(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            connection.Enqueue(new HelloMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            connection.Sync();
            ((ServerInfo)connection.Server).Version = serverVersionCollector.Server;
        }

        public async Task AuthenticateAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var serverVersionCollector = new ServerVersionCollector();
            connection.Enqueue(new HelloMessage(userAgent, authToken.AsDictionary()), serverVersionCollector);
            await connection.SyncAsync().ConfigureAwait(false);
            ((ServerInfo)connection.Server).Version = serverVersionCollector.Server;
        }

        public IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark bookmark, TransactionConfig txConfig)
        {
            var resultBuilder = new ResultBuilder(statement, connection.ReceiveOne, connection.Server, resultResourceHandler);
            connection.Enqueue(new RunWithMetadataMessage(statement, bookmark, txConfig), resultBuilder, PullAll);
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection, Statement statement,
            IResultResourceHandler resultResourceHandler, Bookmark bookmark, TransactionConfig txConfig)
        {
            var resultBuilder = new ResultCursorBuilder(statement.Text, statement.Parameters,
                connection.ReceiveOneAsync, connection.Server, resultResourceHandler);
            connection.Enqueue(new RunWithMetadataMessage(statement, bookmark, txConfig), resultBuilder, PullAll);
            await connection.SendAsync().ConfigureAwait(false);
            return await resultBuilder.PreBuildAsync().ConfigureAwait(false);
        }

        public void BeginTransaction(IConnection connection, Bookmark bookmark)
        {
            throw new System.NotImplementedException();
        }

        public Task BeginTransactionAsync(IConnection connection, Bookmark bookmark)
        {
            throw new System.NotImplementedException();
        }

        public IStatementResult RunInExplicitTransaction(IConnection connection, Statement statement)
        {
            throw new System.NotImplementedException();
        }

        public Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection, Statement statement)
        {
            throw new System.NotImplementedException();
        }

        public Bookmark CommitTransaction(IConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public Task<Bookmark> CommitTransactionAsync(IConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public void RollbackTransaction(IConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public Task RollbackTransactionAsync(IConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public void Reset(IConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}