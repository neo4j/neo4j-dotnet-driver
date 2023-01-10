// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Xunit;

namespace Neo4j.Driver.Tests.Routing
{
    public class ClusterDiscoveryTests
    {
        public class RediscoveryMethod
        {
            [Fact]
            public async Task ShouldParseRoutingTableResult()
            {
                var bookmarks = Bookmarks.From("id1");
                IReadOnlyDictionary<string, object> routingTable = new Dictionary<string, object>
                {
                    ["db"] = "test",
                    ["ttl"] = 15000L,
                    ["servers"] = new List<Dictionary<string, object>>
                    {
                        new()
                        {
                            ["addresses"] = new List<string> { "localhost:7689" },
                            ["role"] = "READ"
                        },
                        new()
                        {
                            ["addresses"] = new List<string> { "anotherServer" },
                            ["role"] = "WRITE"
                        },
                        new()
                        {
                            ["addresses"] = new List<string> { "localhost:7689", "anotherServer" },
                            ["role"] = "ROUTE"
                        }
                    }
                };

                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.GetRoutingTableAsync("test", "fake-person", bookmarks))
                    .ReturnsAsync(routingTable);

                // When
                var manager = new ClusterDiscovery();
                var table = await manager.DiscoverAsync(mockConn.Object, "test", "fake-person", bookmarks);

                // Then
                table.Database.Should().Be("test");
                table.Readers.Should().BeEquivalentTo(new Uri("neo4j://localhost:7689"));
                table.Writers.Should().BeEquivalentTo(new Uri("neo4j://anotherServer:7687"));
                table.Routers.Should()
                    .BeEquivalentTo(
                        new Uri("neo4j://localhost:7689"),
                        new Uri("neo4j://anotherServer:7687"));

                table.ExpireAfterSeconds.Should().Be(15000L);
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

        internal class MockedConnection
        {
            private readonly IResponsePipeline _pipeline = new ResponsePipeline(null);
            private readonly IList<IRequestMessage> _requestMessages = new List<IRequestMessage>();

            private readonly IList<IResponseMessage> _responseMessages = new List<IResponseMessage>();
            private int _requestCount;
            private int _responseCount;

            public MockedConnection(
                AccessMode mode,
                List<Tuple<IRequestMessage, IResponseMessage>> messages,
                ServerInfo serverInfo = null,
                IDictionary<string, string> routingContext = null)
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

                MockConn.Setup(
                        x => x.EnqueueAsync(
                            It.IsAny<IRequestMessage>(),
                            It.IsAny<IResponseHandler>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IRequestMessage, IResponseHandler>(
                        (msg1, handler1) =>
                        {
                            msg1.ToString().Should().Be(_requestMessages[_requestCount].ToString());
                            _requestCount++;
                            _pipeline.Enqueue(handler1);
                        });

                MockConn.Setup(x => x.ReceiveOneAsync())
                    .Returns(
                        () =>
                        {
                            if (_responseCount < _responseMessages.Count)
                            {
                                _responseMessages[_responseCount].Dispatch(_pipeline);
                                _responseCount++;
                                _pipeline.AssertNoFailure();
                                return Task.CompletedTask;
                            }

                            throw new InvalidOperationException("Not enough response message to provide");
                        });

                MockConn.Setup(x => x.SyncAsync())
                    .Returns(
                        () =>
                        {
                            if (_responseCount < _responseMessages.Count)
                            {
                                _responseMessages[_responseCount].Dispatch(_pipeline);
                                _responseCount++;
                                _pipeline.AssertNoFailure();
                                return Task.CompletedTask;
                            }

                            throw new InvalidOperationException("Not enough response message to provide");
                        });

                MockConn.Setup(x => x.IsOpen).Returns(() => _responseCount < _responseMessages.Count);

                MockConn.Setup(x => x.Mode).Returns(mode);

                IBoltProtocol protocol = V3BoltProtocol.Instance;

                if (serverInfo != null)
                {
                    if (serverInfo.Protocol >= BoltProtocolVersion.V4_0)
                    {
                        protocol = BoltProtocol.Instance;
                    }

                    MockConn.Setup(x => x.Server).Returns(serverInfo);
                }
                else
                {
                    MockConn.Setup(x => x.Server)
                        .Returns(new ServerInfo(new Uri("bolt://123:456")) { Agent = "Neo4j/3.5.0" });
                }

                MockConn.Setup(x => x.BoltProtocol).Returns(protocol);
            }

            public Mock<IConnection> MockConn { get; } = new();
        }
    }
}
