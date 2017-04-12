// Copyright (c) 2002-2017 "Neo Technology,"
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
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class ClusterDiscoveryManagerTests
    {
        private static ClusterDiscoveryManager CreateDiscoveryManager(IConnection connection, IDictionary<string, string> context=null, ILogger logger=null)
        {
            return new ClusterDiscoveryManager(connection, context, logger);
        }

        public class RediscoveryMethod
        {
            [Theory]
            [InlineData(1,1,1)]
            [InlineData(2,1,1)]
            [InlineData(1,2,1)]
            [InlineData(2,2,1)]
            [InlineData(1,1,2)]
            [InlineData(2,1,2)]
            [InlineData(1,2,2)]
            [InlineData(2,2,2)]
            [InlineData(3,1,2)]
            [InlineData(3,2,1)]
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
                clientMock.Verify(x=>x.Dispose(), Times.Once);
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
                var exception = Record.Exception(()=>manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ServiceUnavailableException>();
                exception.Message.Should().StartWith("Error when calling `getServers` procedure: ");
                messagingClient.ClientMock.Verify(x=>x.Dispose(), Times.Once);
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
                clientMock.Verify(x=>x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldProtocolErrorWhenMultipleRecord()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var manager = CreateDiscoveryManager(SetupSocketConnection(new List<object[]>
                {
                    CreateGetServersResponseRecordFields(3,2,1),
                    CreateGetServersResponseRecordFields(3,2,1)
                }, clientMock), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Error when parsing `getServers` result: Sequence contains more than one element.");
                clientMock.Verify(x => x.Dispose(), Times.Once);
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
                exception.Message.Should().Be("Error when parsing `getServers` result: keys (2) does not equal to values (1).");
                clientMock.Verify(x => x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfRouterIsEmpty()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var recordFields = CreateGetServersResponseRecordFields(0,2,1);
                var manager = CreateDiscoveryManager(SetupSocketConnection(recordFields, clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(2);
                manager.Routers.Count().Should().Be(0);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("0 routers, 2 writers and 1 readers.");
                clientMock.Verify(x => x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfReaderIsEmpty()
            {
                // Given
                var clientMock = new Mock<ISocketClient>();
                var procedureReplyRecordFields = CreateGetServersResponseRecordFields(3,1,0);
                var manager = CreateDiscoveryManager(SetupSocketConnection(procedureReplyRecordFields, clientMock));

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(0);
                manager.Writers.Count().Should().Be(1);
                manager.Routers.Count().Should().Be(3);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("3 routers, 1 writers and 0 readers.");
                clientMock.Verify(x => x.Dispose(), Times.Once);
            }
        }

        internal static InitMessage InitMessage(IAuthToken auth = null)
        {
            auth = auth ?? AuthTokens.None;
            return new InitMessage(ConnectionSettings.DefaultUserAgent, auth.AsDictionary());
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

        internal static IConnection SetupSocketConnection(List<object[]> recordFieldsList, Mock<ISocketClient> clientMock)
        {
            var pairs = new List<Tuple<IRequestMessage, IResponseMessage>>
            {
                MessagePair(InitMessage(), SuccessMessage()),
                MessagePair(new RunMessage("CALL dbms.cluster.routing.getServers"),
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

        public Mock<ISocketClient> ClientMock { get; }
        public ISocketClient Client => ClientMock.Object;

        /// <summary>
        /// Create a mocked client for testing
        /// </summary>
        /// <param name="requestAndResponsePairs">
        /// The is no one-to-one mapping in the request and response pair.
        /// Only the order of request messages (and/or response messages) that really matters.
        /// However it is suggested to have client request message and the expected server response message matched in a pair to make it easier to read in code.
        /// In case of no matched value for a request (or receive) message, use <c>null</c> as a place holder.
        /// </param>
        /// <param name="clientMock">Set this parameter if you want to pass in the mocked client yourself.</param>
        public MockedMessagingClient(IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs, Mock<ISocketClient> clientMock = null)
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

            ClientMock = clientMock ?? new Mock<ISocketClient>();
            ClientMock.Setup(x => x.Send(It.IsAny<IEnumerable<IRequestMessage>>()))
                .Callback<IEnumerable<IRequestMessage>>(msg => {
                    foreach (var m in msg)
                    {
                        _requestMessages[_requestCount].ToString().Should().Be(m.ToString());
                        _requestCount ++;
                    }
                });

            ClientMock.Setup(x => x.ReceiveOne(It.IsAny<IMessageResponseHandler>()))
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
            ClientMock.Setup(x => x.IsOpen).Returns(() => _responseCount < _responseMessages.Count);
        }
    }
}
