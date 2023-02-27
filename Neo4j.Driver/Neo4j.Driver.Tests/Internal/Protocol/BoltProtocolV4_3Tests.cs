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
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using Record = Xunit.Record;

namespace Neo4j.Driver.Internal.Protocol
{
    public class BoltProtocolV4_3Tests
    {
        private async Task EnqueAndSync(IBoltProtocol protocol)
        {
            var mockConn = new Mock<IConnection>();

            mockConn.Setup(x => x.Server).Returns(new ServerInfo(new Uri("http://neo4j.com")));
            await protocol.LoginAsync(mockConn.Object, "user-andy", AuthTokens.None);

            mockConn.Verify(
                x => x.EnqueueAsync(It.IsAny<HelloMessage>(), It.IsAny<HelloResponseHandler>(), null, null),
                Times.Once);
            mockConn.Verify(x => x.SyncAsync());
        }

        [Fact]
        public async Task ShouldEnqueueHelloAndSync()
        {
            var protocol = new BoltProtocolV4_3(new Dictionary<string, string> { { "ContextKey", "ContextValue" } });

            await EnqueAndSync(protocol);
        }

        [Fact]
        public async Task ShouldEnqueueHelloAndSyncEmptyContext()
        {
            var protocol = new BoltProtocolV4_3(new Dictionary<string, string>());

            await EnqueAndSync(protocol);
        }

        [Fact]
        public async void ShouldEnqueueHelloAndSyncNullContext()
        {
            var protocol = new BoltProtocolV4_3(null);

            await EnqueAndSync(protocol);
        }

        [Fact]
        public async Task GetRoutingTableShouldThrowOnNullConnectionObject()
        {
            var protocol = new BoltProtocolV4_3(new Dictionary<string, string> { { "ContextKey", "ContextValue" } });

            var ex = await Xunit.Record.ExceptionAsync(async () => await protocol.GetRoutingTable(null, "adb", null, null));

            ex.Should().BeOfType<ProtocolException>().Which
                .Message.Should()
                .Contain("Attempting to get a routing table on a null connection");
        }

		[Fact]
		public async Task RoutingTableShouldContainDatabaseName()
		{
			var databaseName = "myDatabaseName";
			var protocol = new BoltProtocolV4_3(new Dictionary<string, string> { { "ContextKey", "ContextValue" } });
			var connection = SetupMockedConnection(databaseName);

			var routingTable = await protocol.GetRoutingTable(connection, databaseName, null, null);

			routingTable.ToDictionary().Should().ContainKey("db").WhichValue.Should().Be(databaseName);
		}

		[Fact]
		public async Task ShouldCloseConnectionWhenThrows()
		{

			var databaseName = "myDatabaseName";
			var protocol = new BoltProtocolV4_3(new Dictionary<string, string> { { "ContextKey", "ContextValue" } });
			var routingContext = new Dictionary<string, string>
			{
				{"name", "molly"},
				{"age", "1"},
				{"color", "white"}
			};

			var mockConn = Tests.Routing.ClusterDiscoveryTests.Setup43SocketConnection(routingContext, 
				databaseName, 
				null, 
				Tests.Routing.ClusterDiscoveryTests.CreateGetServersDictionary(1, 1, 1));

			mockConn.Setup(x => x.SyncAsync()).Throws(new FatalDiscoveryException("database doesn't exist"));
			
			mockConn.Setup(m => m.RoutingContext).Returns(routingContext);
			var exception = await Record.ExceptionAsync(() => protocol.GetRoutingTable(mockConn.Object, databaseName, null, null));

			exception.Should().BeOfType<FatalDiscoveryException>();
			mockConn.Verify(x => x.CloseAsync(), Times.Once);
		}
		
		private IConnection SetupMockedConnection(string databaseName)
		{
			//Given
			var routingContext = new Dictionary<string, string>
				{
					{"name", "molly"},
					{"age", "1"},
					{"color", "white"}
				};

			var mockConn = Tests.Routing.ClusterDiscoveryTests.Setup43SocketConnection(routingContext, 
																					   databaseName, 
																					   null, 
																					   Tests.Routing.ClusterDiscoveryTests.CreateGetServersDictionary(1, 1, 1));
			mockConn.Setup(m => m.RoutingContext).Returns(routingContext);

			return mockConn.Object;
		}

	}
}
