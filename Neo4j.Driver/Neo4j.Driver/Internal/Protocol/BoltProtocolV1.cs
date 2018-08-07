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

using System.IO;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV1 : IBoltProtocol
    {
        public static readonly BoltProtocolV1 BoltV1 = new BoltProtocolV1();

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger=null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize,
                bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V1);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize,
                bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V1);
        }
        
        public void InitializeConnection(IConnection connection, string userAgent, IAuthToken authToken)
        {
            var initCollector = new InitCollector();
            connection.Enqueue(new InitMessage(userAgent, authToken.AsDictionary()), initCollector);
            connection.Sync();
            ((ServerInfo)connection.Server).Version = initCollector.Server;
        }

        public void BeginTransaction(IConnection connection, IConnectionContext context)
        {
        }

        public Bookmark CommitTransaction(IConnection connection, IConnectionContext context)
        {
            throw new System.NotImplementedException();
        }

        public void RollbackTransaction(IConnection connection, IConnectionContext context)
        {
            throw new System.NotImplementedException();
        }

        public IStatementResult RunInAutoCommitTransaction(IConnection connection, Statement statement, IResultResourceHandler resultResourceHandler)
        {
            var resultBuilder = new ResultBuilder(statement.Text, statement.Parameters,
                connection.ReceiveOne, connection.Server, resultResourceHandler);
            connection.Enqueue(new RunMessage(statement), resultBuilder, new PullAllMessage());
            connection.Send();
            return resultBuilder.PreBuild();
        }

        public IStatementResult RunInExplicitTransaction(IConnection connection, IConnectionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
