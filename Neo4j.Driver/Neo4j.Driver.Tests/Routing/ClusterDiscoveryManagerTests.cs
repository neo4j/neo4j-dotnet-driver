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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;
using static Neo4j.Driver.Tests.Routing.MockedMessagingClient;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Routing
{
    public class ClusterDiscoveryManagerTests
    {
        private static ClusterDiscoveryManager CreateDiscoveryManager(IConnection connection,
            IDictionary<string, string> context = null, ILogger logger = null)
        {
            return new ClusterDiscoveryManager(connection, context, logger);
        }

        public class Constructor
        {
            [Theory]
            [InlineData("Neo4j/1.9.9")]
            [InlineData("1.9.9")]
            [InlineData("Neo4j/3.1.9")]
            [InlineData("3.1.9")]
            public void ShouldUseGetServersProceudre(string version)
            {
                // Given
                var mock = new Mock<IConnection>();
                var serverInfoMock = new Mock<IServerInfo>();
                serverInfoMock.Setup(m => m.Version).Returns(version);
                mock.Setup(m => m.Server).Returns(serverInfoMock.Object);
                // When
                var discoveryManager = new ClusterDiscoveryManager(mock.Object, null, null);
                // Then
                discoveryManager.DiscoveryProcedure.Text.Should().Be("CALL dbms.cluster.routing.getServers");
                discoveryManager.DiscoveryProcedure.Parameters.Should().BeEmpty();
            }

            [Theory]
            [InlineData("Neo4j/3.2.0-alpha01")]
            [InlineData("3.2.0-alpha01")]
            [InlineData("Neo4j/3.2.1")]
            [InlineData("3.2.1")]
            public void ShouldUseGetRoutingTableProceudre(string version)
            {
                // Given
                var mock = new Mock<IConnection>();
                var serverInfoMock = new Mock<IServerInfo>();
                serverInfoMock.Setup(m => m.Version).Returns(version);
                mock.Setup(m => m.Server).Returns(serverInfoMock.Object);
                // When
                var context = new Dictionary<string, string> {{"context", string.Empty}};
                var discoveryManager = new ClusterDiscoveryManager(mock.Object, context, null);
                // Then
                discoveryManager.DiscoveryProcedure.Text.Should()
                    .Be("CALL dbms.cluster.routing.getRoutingTable({context})");
                discoveryManager.DiscoveryProcedure.Parameters["context"].Should().Be(context);
            }
        }

        public class RediscoveryMethod
        {
            [Theory]
            [InlineData(1, 1, 1)]
            [InlineData(2, 1, 1)]
            [InlineData(1, 2, 1)]
            [InlineData(2, 2, 1)]
            [InlineData(1, 1, 2)]
            [InlineData(2, 1, 2)]
            [InlineData(1, 2, 2)]
            [InlineData(2, 2, 2)]
            [InlineData(3, 1, 2)]
            [InlineData(3, 2, 1)]
            public void ShouldCarryOutRediscoveryWith32Server(int routerCount, int writerCount, int readerCount)
            {
                // Given
                var routingContext = new Dictionary<string, string>
                {
                    {"name", "molly"},
                    {"age", "1"},
                    {"color", "whilte"}
                };
                var recordFields = CreateGetServersResponseRecordFields(routerCount, writerCount, readerCount);
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(Setup32SocketConnection(routingContext, recordFields, clientMock),
                    routingContext);

                // When
                manager.Rediscovery();

                // Then
                manager.Readers.Count().Should().Be(readerCount);
                manager.Writers.Count().Should().Be(writerCount);
                manager.Routers.Count().Should().Be(routerCount);
                manager.ExpireAfterSeconds = 9223372036854775807;
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Theory]
            [InlineData(1, 1, 1)]
            [InlineData(2, 1, 1)]
            [InlineData(1, 2, 1)]
            [InlineData(2, 2, 1)]
            [InlineData(1, 1, 2)]
            [InlineData(2, 1, 2)]
            [InlineData(1, 2, 2)]
            [InlineData(2, 2, 2)]
            [InlineData(3, 1, 2)]
            [InlineData(3, 2, 1)]
            public void ShouldCarryOutRediscovery(int routerCount, int writerCount, int readerCount)
            {
                // Given
                var recordFields = CreateGetServersResponseRecordFields(routerCount, writerCount, readerCount);
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(SetupSocketConnection(recordFields, clientMock));

                // When
                manager.Rediscovery();

                // Then
                manager.Readers.Count().Should().Be(readerCount);
                manager.Writers.Count().Should().Be(writerCount);
                manager.Routers.Count().Should().Be(routerCount);
                manager.ExpireAfterSeconds = 9223372036854775807;
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldServiceUnavailableWhenProcedureNotFound()
            {
                // Given
                var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
                {
                    MessagePair(InitMessage(), SuccessMessage()),
                    MessagePair(new RunMessage("CALL dbms.cluster.routing.getServers"),
                        new FailureMessage("Neo.ClientError.Procedure.ProcedureNotFound", "not found")),
                    MessagePair(PullAllMessage(), new IgnoredMessage())
                };

                var messagingClient = new MockedMessagingClient(pairs);
                var conn = SocketConnectionTests.NewSocketConnection(messagingClient.Client);
                conn.Init();

                var manager = CreateDiscoveryManager(conn, null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ServiceUnavailableException>();
                exception.Message.Should().StartWith("Error when calling `getServers` procedure: ");
                messagingClient.ClientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldProtocolErrorWhenNoRecord()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(SetupSocketConnection(new List<object[]>(), clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Error when parsing `getServers` result: Sequence contains no elements.");
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldProtocolErrorWhenMultipleRecord()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(SetupSocketConnection(new List<object[]>
                {
                    CreateGetServersResponseRecordFields(3, 2, 1),
                    CreateGetServersResponseRecordFields(3, 2, 1)
                }, clientMock), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should()
                    .Be("Error when parsing `getServers` result: Sequence contains more than one element.");
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldProtocolErrorWhenRecordUnparsable()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(SetupSocketConnection(new object[] {1}, clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should()
                    .Be("Error when parsing `getServers` result: keys (2) does not equal to values (1).");
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfRouterIsEmpty()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var recordFields = CreateGetServersResponseRecordFields(0, 2, 1);
                var manager = CreateDiscoveryManager(SetupSocketConnection(recordFields, clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(2);
                manager.Routers.Count().Should().Be(0);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("0 routers, 2 writers and 1 readers.");
                clientMock.Verify(x => x.Stop(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfReaderIsEmpty()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var procedureReplyRecordFields = CreateGetServersResponseRecordFields(3, 1, 0);
                var manager = CreateDiscoveryManager(SetupSocketConnection(procedureReplyRecordFields, clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(0);
                manager.Writers.Count().Should().Be(1);
                manager.Routers.Count().Should().Be(3);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("3 routers, 1 writers and 0 readers.");
                clientMock.Verify(x => x.Stop(), Times.Once);
            }
        }

        public class BoltRoutingUriMethod
        {
            [Theory]
            [InlineData("localhost", "localhost", GraphDatabase.DefaultBoltPort)]
            [InlineData("localhost:9193", "localhost", 9193)]
            [InlineData("neo4j.com", "neo4j.com", GraphDatabase.DefaultBoltPort)]
            [InlineData("royal-server.com.uk", "royal-server.com.uk", GraphDatabase.DefaultBoltPort)]
            [InlineData("royal-server.com.uk:4546", "royal-server.com.uk", 4546)]
            // IPv4
            [InlineData("127.0.0.1", "127.0.0.1", GraphDatabase.DefaultBoltPort)]
            [InlineData("8.8.8.8:8080", "8.8.8.8", 8080)]
            [InlineData("0.0.0.0", "0.0.0.0", GraphDatabase.DefaultBoltPort)]
            [InlineData("192.0.2.235:4329", "192.0.2.235", 4329)]
            [InlineData("172.31.255.255:255", "172.31.255.255", 255)]
            // IPv6
            [InlineData("[1afc:0:a33:85a3::ff2f]", "[1afc:0:a33:85a3::ff2f]", GraphDatabase.DefaultBoltPort)]
            [InlineData("[::1]:1515", "[::1]", 1515)]
            [InlineData("[ff0a::101]:8989", "[ff0a::101]", 8989)]
            // IPv6 with zone id
            [InlineData("[1afc:0:a33:85a3::ff2f%eth1]", "[1afc:0:a33:85a3::ff2f]", GraphDatabase.DefaultBoltPort)]
            [InlineData("[::1%eth0]:3030", "[::1]", 3030)]
            [InlineData("[ff0a::101%8]:4040", "[ff0a::101]", 4040)]
            public void ShouldHaveLocalhost(string input, string host, int port)
            {
                var uri = ClusterDiscoveryManager.BoltRoutingUri(input);
                uri.Scheme.Should().Be("bolt+routing");
                uri.Host.Should().Be(host);
                uri.Port.Should().Be(port);
            }
        }

        internal static object[] CreateGetServersResponseRecordFields(int routerCount, int writerCount, int readerCount)
        {
            return new object[]
            {
                "9223372036854775807",
                new List<object>
                {
                    new Dictionary<string, object>
                    {
                        {"addresses", GenerateServerList(routerCount)},
                        {"role", "ROUTE"}
                    },
                    new Dictionary<string, object>
                    {
                        {"addresses", GenerateServerList(writerCount)},
                        {"role", "WRITE"}
                    },
                    new Dictionary<string, object>
                    {
                        {"addresses", GenerateServerList(readerCount)},
                        {"role", "READ"}
                    }
                }
            };
        }

        private static IList<object> GenerateServerList(int count)
        {
            var list = new List<object>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add($"127.0.0.1:{i + 9001}");
            }
            return list;
        }

        internal static IConnection SetupSocketConnection(object[] recordFileds, Mock<ISocketClient> clientMock)
        {
            return SetupSocketConnection(new List<object[]> {recordFileds}, clientMock);
        }

        internal static IConnection Setup32SocketConnection(IDictionary<string, string> routingContext,
            object[] recordFields, Mock<ISocketClient> clientMock)
        {
            var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(InitMessage(), SuccessMessage()),
                MessagePair(new RunMessage("CALL dbms.cluster.routing.getRoutingTable({context})",
                        new Dictionary<string, object> {{"context", routingContext}}),
                    SuccessMessage(new List<object> {"ttl", "servers"})),
                MessagePair(new RecordMessage(recordFields)),
                MessagePair(PullAllMessage(), SuccessMessage())
            };

            var mock = new MockedMessagingClient(pairs, clientMock);
            var serverInfo = new ServerInfo(new Uri("bolt://123:456"));
            var conn = SocketConnectionTests.NewSocketConnection(mock.Client, server: serverInfo);
            conn.Init();
            serverInfo.Version = "Neo4j/3.2.2";
            return conn;
        }

        internal static IConnection SetupSocketConnection(List<object[]> recordFieldsList,
            Mock<ISocketClient> clientMock)
        {
            var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(InitMessage(), SuccessMessage()),
                MessagePair(new RunMessage("CALL dbms.cluster.routing.getServers", new Dictionary<string, object>()),
                    SuccessMessage(new List<object> {"ttl", "servers"}))
            };

            foreach (var recordFields in recordFieldsList)
            {
                pairs.Add(MessagePair(new RecordMessage(recordFields)));
            }
            pairs.Add(MessagePair(PullAllMessage(), SuccessMessage()));

            var mock = new MockedMessagingClient(pairs, clientMock);
            var conn = SocketConnectionTests.NewSocketConnection(mock.Client);
            conn.Init();
            return conn;
        }
    }
}
