// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V4_1;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Tests;
using Xunit;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolUtils;
using static Neo4j.Driver.Internal.Protocol.BoltProtocolV4_1;
using V4_1 = Neo4j.Driver.Internal.MessageHandling.V4_1;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolV4_1Tests
    {
        private async Task EnqueAndSync(IBoltProtocol V4_1)
        {
            var mockConn = new Mock<IConnection>();

            mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
            await V4_1.LoginAsync(mockConn.Object, "user-andy", AuthTokens.None);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsAny<Messaging.V4_1.HelloMessage>(), It.IsAny<V4_1.HelloResponseHandler>(), null, null),
				Times.Once);
            mockConn.Verify(x => x.SyncAsync());
        }

        [Fact]
        public async Task ShouldEnqueueHelloAndSync()
        {            
            var V4_1 = new BoltProtocolV4_1(new Dictionary<string, string> { { "ContextKey", "ContextValue" } });

            await EnqueAndSync(V4_1);
        }

        [Fact]
        public async Task ShouldEnqueueHelloAndSyncEmptyContext()
        {
            var V4_1 = new BoltProtocolV4_1(new Dictionary<string, string>());

            await EnqueAndSync(V4_1);
        }

        [Fact]
        public async void ShouldEnqueueHelloAndSyncNullContext()
        {
            var V4_1 =  new BoltProtocolV4_1(null);

            await EnqueAndSync(V4_1);
        }

    }
}
