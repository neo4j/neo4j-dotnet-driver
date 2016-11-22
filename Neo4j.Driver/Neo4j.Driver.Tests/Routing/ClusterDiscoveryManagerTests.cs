// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class ClusterDiscoveryManagerTests
    {
        public class RediscoveryMethod
        {
            [Fact]
            public void ShouldCarryOutRediscovery()
            {
                // Given
                var procedureReplyRecordFields = new object[]
                {
                    "9223372036854775807",
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9001", "127.0.0.1:9002", "127.0.0.1:9003"}},
                            {"role", "ROUTE"}
                        },
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9001", "127.0.0.1:9002"}},
                            {"role", "WRITE"}
                        },
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9003"}},
                            {"role", "READ"}
                        }
                    }
                };
                var manager = new ClusterDiscoveryManager(SetupConnection(procedureReplyRecordFields), null);

                // When
                manager.Rediscovery();

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(2);
                manager.Routers.Count().Should().Be(3);
                manager.ExpireAfterSeconds = 9223372036854775807;
            }

            [Fact]
            public void ShouldThrowExceptionIfRouterIsEmpty()
            {
                // Given
                var procedureReplyRecordFields = new object[]
                {
                    "9223372036854775807",
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9001", "127.0.0.1:9002"}},
                            {"role", "WRITE"}
                        },
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9003"}},
                            {"role", "READ"}
                        }
                    }
                };
                var manager = new ClusterDiscoveryManager(SetupConnection(procedureReplyRecordFields), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(2);
                manager.Routers.Count().Should().Be(0);
                exception.Should().BeOfType<InvalidDiscoveryException>();
                exception.Message.Should().Contain("0 routers, 2 writers and 1 readers.");
            }

            [Fact]
            public void ShouldThrowExceptionIfWriterIsEmpty()
            {
                // Given
                var procedureReplyRecordFields = new object[]
                {
                    "9223372036854775807",
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9001", "127.0.0.1:9002", "127.0.0.1:9003"}},
                            {"role", "ROUTE"}
                        },
                        new Dictionary<string, object>
                        {
                            {"addresses", new List<object> {"127.0.0.1:9003"}},
                            {"role", "READ"}
                        }
                    }
                };
                var manager = new ClusterDiscoveryManager(SetupConnection(procedureReplyRecordFields), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(0);
                manager.Routers.Count().Should().Be(3);
                exception.Should().BeOfType<InvalidDiscoveryException>();
                exception.Message.Should().Contain("3 routers, 0 writers and 1 readers.");
            }
        }

        internal static InitMessage InitMessage(IAuthToken auth = null)
        {
            auth = auth ?? AuthTokens.None;
            return new InitMessage("neo4j-dotnet/1.1", auth.AsDictionary());
        }

        internal static SuccessMessage SuccessMessage(IList<object> fileds = null)
        {
            return fileds == null
                ? new SuccessMessage(new Dictionary<string, object>())
                : new SuccessMessage(new Dictionary<string, object> {{"fields", fileds}});
        }

        internal static PullAllMessage PullAllMessage()
        {
            return new PullAllMessage();
        }

        internal static Tuple<IRequestMessage, IResponseMessage> MessagePair(IRequestMessage request, IResponseMessage response)
        {
            return new Tuple<IRequestMessage, IResponseMessage>(request, response);
        }

        internal static Tuple<IRequestMessage, IResponseMessage> MessagePair(IResponseMessage response)
        {
            return MessagePair(null, response);
        }

        internal static IConnection SetupConnection(object[] recordFields)
        {
            var requestAndResponsePairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(InitMessage(), SuccessMessage()),
                MessagePair(new RunMessage("CALL dbms.cluster.routing.getServers"),
                    SuccessMessage(new List<object> {"ttl", "servers"})),
                MessagePair(new RecordMessage(recordFields)),
                MessagePair(PullAllMessage(), SuccessMessage())
            };
            var mock = new MockedMessagingClient(requestAndResponsePairs);
            var conn = new SocketConnection(mock.Client, AuthTokens.None, null, new ServerInfo(new Uri("http://1234.com")));
            conn.Init();
            return conn;
        }
    }

    /// <summary>
    /// If you want to create a connection with full control of what messages to send and what messages to receive,
    /// you will find this mocked client very useful.
    /// This client provides a clear cut from encoding/decoding messages and writing/reading from a real socket.
    /// Instead, you talk messages directly. 
    /// When creating a mocked client, you provide what you expect this client to send and what you want the server to response.
    /// When sending a request message via this client, it verifies that the message to send is the same as expected.
    /// Then when the driver wants to consume a response from the server, the response message specified at the initialization will be replied in order.
    /// </summary>
    internal class MockedMessagingClient
    {
        private readonly IList<IRequestMessage> _requestMessages = new List<IRequestMessage>();
        private int _requestCount;
        private readonly IList<IResponseMessage> _responseMessages = new List<IResponseMessage>();
        private int _responseCount;

        public IList<IRequestMessage> SendMessages => new List<IRequestMessage>(_requestMessages);
        public IList<IResponseMessage> ReceiveMessages => new List<IResponseMessage>(_responseMessages);

        public Mock<ISocketClient> MockedClient { get; }
        public ISocketClient Client => MockedClient.Object;

        /// <summary>
        /// Create a mocked client for testing
        /// </summary>
        /// <param name="requestAndResponsePairs">
        /// The is no one-to-one mapping in the request and response pair.
        /// Only the order of request messages (and/or response messages) that really matters.
        /// However it is suggested to have client request message and the expected server response message matched in a pair to make it easier to read in code.
        /// In case of no matched value for a request (or receive) message, use <c>null</c> as a place holder.
        /// </param>
        public MockedMessagingClient(IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs)
        {
            foreach (var pair in requestAndResponsePairs)
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

            MockedClient = new Mock<ISocketClient>();
            MockedClient.Setup(x => x.Send(It.IsAny<IEnumerable<IRequestMessage>>()))
                .Callback<IEnumerable<IRequestMessage>>(msg => {
                    foreach (var m in msg)
                    {
                        _requestMessages[_requestCount].ToString().Should().Be(m.ToString());
                        _requestCount ++;
                    }
                });

            MockedClient.Setup(x => x.ReceiveOne(It.IsAny<IMessageResponseHandler>()))
                .Callback<IMessageResponseHandler>(handler =>
                {
                    if (_responseCount < _responseMessages.Count)
                    {
                        _responseMessages[_responseCount].Dispatch(handler);
                        _responseCount ++;
                    }
                    else
                    {
                        throw new InvalidOperationException("Not enough response message to provide");
                    }

                });
            MockedClient.Setup(x => x.IsOpen).Returns(() => _responseCount < _responseMessages.Count);
        }
    }
}
