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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V1;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Messaging.V4;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;

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

//        public async Task<IStatementResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
//            Statement statement,
//            IResultResourceHandler resultResourceHandler, Bookmark bookmark, TransactionConfig txConfig)
//        {
//            var resultBuilder = new ResultCursorBuilder(NewSummaryCollector(statement, connection.Server),
//                connection.ReceiveOneAsync, resultResourceHandler);
//            await connection.EnqueueAsync(new RunWithMetadataMessage(statement, bookmark, txConfig), resultBuilder,
//                new PullNMessage(All)).ConfigureAwait(false);
//            await connection.SendAsync().ConfigureAwait(false);
//            return resultBuilder.PreBuild();
//        }
//
//        public async Task<IStatementResultCursor> RunInExplicitTransactionAsync(IConnection connection,
//            Statement statement)
//        {
//            var resultBuilder = new ResultCursorBuilder(
//                NewSummaryCollector(statement, connection.Server), connection.ReceiveOneAsync);
//            await connection.EnqueueAsync(new RunWithMetadataMessage(statement), resultBuilder, new PullNMessage(All))
//                .ConfigureAwait(false);
//            await connection.SendAsync().ConfigureAwait(false);
//
//            return resultBuilder.PreBuild();
//        }
    }
}