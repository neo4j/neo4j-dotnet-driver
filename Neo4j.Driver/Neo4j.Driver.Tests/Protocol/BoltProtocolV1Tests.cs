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

using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class BoltProtocolV1Tests
    {
        public class RunInAutoCommitTransactionMethod
        {        
            [Fact]
            public void ShouldEnqueueRunAndPullAllAndSend()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                BoltProtocolV1.BoltV1.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object);

                mockConn.Verify(x => x.Enqueue(It.IsAny<RunMessage>(), It.IsAny<ResultBuilder>(), It.IsAny<PullAllMessage>()), Times.Once);
                mockConn.Verify(x => x.Send());
            }

            [Fact]
            public void ResultBuilderShouldObtainServerInfoFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var statement = new Statement("A cypher query");
                var mockHandler = new Mock<IResultResourceHandler>();
                BoltProtocolV1.BoltV1.RunInAutoCommitTransaction(mockConn.Object, statement, mockHandler.Object);

                mockConn.Verify(x => x.Server, Times.Once);
            }
        }

        public class InitializeConnectionMethod
        {
            [Fact]
            public void ShouldEnqueueInitAndSync()
            {
                var mockConn = new Mock<IConnection>();
                var mockAuth = new Mock<IAuthToken>();
                BoltProtocolV1.BoltV1.InitializeConnection(mockConn.Object, "user-zhen", mockAuth.Object);

                mockConn.Verify(x => x.Enqueue(It.IsAny<InitMessage>(), It.IsAny<InitCollector>(), null), Times.Once);
                mockConn.Verify(x => x.Sync());
            }
        }
    }
}