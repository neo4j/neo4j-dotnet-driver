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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Messaging.V4;
using Neo4j.Driver.Internal.Util;
using Xunit;
using static Neo4j.Driver.Internal.Messaging.IgnoredMessage;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using static Neo4j.Driver.Tests.Routing.MockedMessagingClient;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests.Routing
{
    public class ClusterDiscoveryTests
    {
        public class Constructor
        {
            [Theory]
            [InlineData("Neo4j/3.2.0-alpha01")]
            [InlineData("3.2.0-alpha01")]
            [InlineData("Neo4j/3.2.1")]
            [InlineData("3.2.1")]
            public void ShouldUseGetRoutingTableProcedure(string version)
            {
                // Given
                var context = new Dictionary<string, string> {{"context", string.Empty}};
                var discovery = new ClusterDiscovery(context, null);
                var mock = new Mock<IConnection>();
                var serverInfoMock = new Mock<IServerInfo>();
                serverInfoMock.Setup(m => m.Version).Returns(version);
                mock.Setup(m => m.Server).Returns(serverInfoMock.Object);
                // When
                var statement = discovery.DiscoveryProcedure(mock.Object, null);
                // Then
                statement.Text.Should()
                    .Be("CALL dbms.cluster.routing.getRoutingTable($context)");
                statement.Parameters["context"].Should().Be(context);
            }

            [Theory]
            [InlineData("Neo4j/4.0.0-alpha01")]
            [InlineData("4.0.0-alpha01")]
            [InlineData("Neo4j/4.0.0")]
            [InlineData("4.0.0")]
            [InlineData("Neo4j/4.0.1")]
            [InlineData("4.0.1")]
            public void ShouldUseGetRoutingTableForDatabaseProcedure(string version)
            {
                // Given
                var context = new Dictionary<string, string> {{"context", string.Empty}};
                var discovery = new ClusterDiscovery(context, null);
                var mock = new Mock<IConnection>();
                var serverInfoMock = new Mock<IServerInfo>();
                serverInfoMock.Setup(m => m.Version).Returns(version);
                mock.Setup(m => m.Server).Returns(serverInfoMock.Object);
                // When
                var statement = discovery.DiscoveryProcedure(mock.Object, "foo");
                // Then
                statement.Text.Should()
                    .Be("CALL dbms.routing.getRoutingTable($context, $database)");
                statement.Parameters["context"].Should().Be(context);
                statement.Parameters["database"].Should().Be("foo");
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
            public async Task ShouldCarryOutRediscoveryWith32Server(int routerCount, int writerCount, int readerCount)
            {
                // Given
                var routingContext = new Dictionary<string, string>
                {
                    {"name", "molly"},
                    {"age", "1"},
                    {"color", "white"}
                };
                var recordFields = CreateGetServersResponseRecordFields(routerCount, writerCount, readerCount);
                var mockConn = Setup32SocketConnection(routingContext, recordFields);
                var manager = new ClusterDiscovery(routingContext, null);

                // When
                var table = await manager.DiscoverAsync(mockConn.Object, null, Bookmark.Empty);

                // Then
                table.Readers.Count().Should().Be(readerCount);
                table.Writers.Count().Should().Be(writerCount);
                table.Routers.Count().Should().Be(routerCount);
                table.ExpireAfterSeconds.Should().Be(15000L);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Theory]
            [InlineData(1, 1, 1, null)]
            [InlineData(2, 1, 1, null, "bookmark-1", "bookmark-2")]
            [InlineData(1, 2, 1, "foo-db")]
            [InlineData(2, 2, 1, "bar-db", "bookmark-3")]
            [InlineData(1, 1, 2, "")]
            [InlineData(2, 1, 2, "")]
            [InlineData(1, 2, 2, "my-db", "bookmark-1", "bookmark-2", "bookmark-3")]
            [InlineData(2, 2, 2, "my-db")]
            [InlineData(3, 1, 2, "that-db")]
            [InlineData(3, 2, 1, "another-db", "bookmark-6")]
            public async Task ShouldCarryOutRediscoveryWith40Server(int routerCount, int writerCount, int readerCount,
                string database, params string[] bookmarks)
            {
                // Given
                var routingContext = new Dictionary<string, string>
                {
                    {"name", "molly"},
                    {"age", "1"},
                    {"color", "white"}
                };
                var recordFields = CreateGetServersResponseRecordFields(routerCount, writerCount, readerCount);
                var mockConn =
                    Setup40SocketConnection(routingContext, database, Bookmark.From(bookmarks), recordFields);
                var manager = new ClusterDiscovery(routingContext, null);

                // When
                var table = await manager.DiscoverAsync(mockConn.Object, database, Bookmark.From(bookmarks));

                // Then
                table.Database.Should().Be(database ?? "");
                table.Readers.Count().Should().Be(readerCount);
                table.Writers.Count().Should().Be(writerCount);
                table.Routers.Count().Should().Be(routerCount);
                table.ExpireAfterSeconds.Should().Be(15000L);
                mockConn.Verify(x => x.CloseAsync(), Times.Once);
            }

            [Fact]
            public void ShouldThrowWhenProcedureNotFound()
            {
                // Given
                var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
                {
                    MessagePair(
                        new RunWithMetadataMessage(new Statement("CALL dbms.cluster.routing.getRoutingTable($context)",
                            new Dictionary<string, object> {{"context", null}}), AccessMode.Write),
                        new FailureMessage("Neo.ClientError.Procedure.ProcedureNotFound", "not found")),
                    MessagePair(PullAll, Ignored)
                };

                var connMock = new MockedConnection(AccessMode.Write, pairs).MockConn;
                var manager = new ClusterDiscovery(null, null);

                // When & Then
                manager.Awaiting(m => m.DiscoverAsync(connMock.Object, null, Bookmark.Empty)).Should()
                    .Throw<ClientException>().WithMessage("*not found*");
                connMock.Verify(x => x.CloseAsync(), Times.Once);
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
                var uri = ClusterDiscovery.BoltRoutingUri(input);
                uri.Scheme.Should().Be("neo4j");
                uri.Host.Should().Be(host);
                uri.Port.Should().Be(port);
            }
        }

        private static object[] CreateGetServersResponseRecordFields(int routerCount, int writerCount, int readerCount)
        {
            return new object[]
            {
                "15000",
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

        private static Mock<IConnection> Setup32SocketConnection(IDictionary<string, string> routingContext,
            object[] recordFields)
        {
            var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(new RunWithMetadataMessage(new Statement(
                        "CALL dbms.cluster.routing.getRoutingTable($context)",
                        new Dictionary<string, object> {{"context", routingContext}}), AccessMode.Write),
                    SuccessMessage(new List<object> {"ttl", "servers"})),
                MessagePair(new RecordMessage(recordFields)),
                MessagePair(PullAll, SuccessMessage())
            };

            var serverInfo = new ServerInfo(new Uri("bolt://123:456")) {Version = "Neo4j/3.2.2"};

            return new MockedConnection(AccessMode.Write, pairs, serverInfo).MockConn;
        }

        private static Mock<IConnection> Setup40SocketConnection(IDictionary<string, string> routingContext,
            string database, Bookmark bookmark, object[] recordFields)
        {
            var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(new RunWithMetadataMessage(new Statement(
                            "CALL dbms.routing.getRoutingTable($context, $database)",
                            new Dictionary<string, object>
                            {
                                {"context", routingContext},
                                {"database", string.IsNullOrEmpty(database) ? null : database},
                            }),
                        "system",
                        bookmark, TransactionOptions.Empty, AccessMode.Read),
                    SuccessMessage(new List<object> {"ttl", "servers"})),
                MessagePair(new RecordMessage(recordFields)),
                MessagePair(new PullMessage(PullMessage.All), SuccessMessage())
            };

            var serverInfo = new ServerInfo(new Uri("bolt://123:456")) {Version = "Neo4j/4.0.0"};

            return new MockedConnection(AccessMode.Read, pairs, serverInfo).MockConn;
        }

        internal class MockedConnection
        {
            private readonly Mock<IConnection> _mockConn = new Mock<IConnection>();
            private readonly IList<IRequestMessage> _requestMessages = new List<IRequestMessage>();
            private int _requestCount;
            private readonly IResponsePipeline _pipeline = new ResponsePipeline(null);

            private readonly IList<IResponseMessage> _responseMessages = new List<IResponseMessage>();
            private int _responseCount;

            public MockedConnection(AccessMode mode, List<Tuple<IRequestMessage, IResponseMessage>> messages,
                ServerInfo serverInfo = null)
            {
                foreach (var pair in messages)
                {
                    if (pair.Item1 != null)
                    {
                        _requestMessages.Add(pair.Item1);
                    }

                    if (pair.Item2 != null)
                    {
                        _responseMessages.Add(pair.Item2);
                    }
                }

                _mockConn.Setup(x => x.EnqueueAsync(It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>(),
                        It.IsAny<IRequestMessage>(), It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler, IRequestMessage, IResponseHandler>(
                        (msg1, handler1, msg2, handler2) =>
                        {
                            msg1.ToString().Should().Be(_requestMessages[_requestCount].ToString());
                            _requestCount++;
                            _pipeline.Enqueue(msg1, handler1);

                            if (msg2 != null)
                            {
                                msg2.ToString().Should().Be(_requestMessages[_requestCount].ToString());
                                _requestCount++;
                                _pipeline.Enqueue(msg2, handler2);
                            }
                        });
                _mockConn.Setup(x => x.ReceiveOneAsync())
                    .Returns(() =>
                    {
                        if (_responseCount < _responseMessages.Count)
                        {
                            _responseMessages[_responseCount].Dispatch(_pipeline);
                            _responseCount++;
                            _pipeline.AssertNoFailure();
                            return Task.CompletedTask;
                        }
                        else
                        {
                            throw new InvalidOperationException("Not enough response message to provide");
                        }
                    });

                _mockConn.Setup(x => x.IsOpen).Returns(() => _responseCount < _responseMessages.Count);
                _mockConn.Setup(x => x.Mode).Returns(mode);
                var protocol = BoltProtocolV3.BoltV3;
                if (serverInfo != null)
                {
                    if (ServerVersion.From(serverInfo.Version) >= ServerVersion.V4_0_0)
                    {
                        protocol = BoltProtocolV4.BoltV4;
                    }

                    _mockConn.Setup(x => x.Server).Returns(serverInfo);
                }
                else
                {
                    _mockConn.Setup(x => x.Server)
                        .Returns(new ServerInfo(new Uri("bolt://123:456")) {Version = "Neo4j/3.5.0"});
                }

                _mockConn.Setup(x => x.BoltProtocol).Returns(protocol);
            }

            public Mock<IConnection> MockConn => _mockConn;
        }
    }
}