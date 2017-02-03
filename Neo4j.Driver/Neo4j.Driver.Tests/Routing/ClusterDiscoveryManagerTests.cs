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
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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
                var manager = new ClusterDiscoveryManager(SetupConnection(recordFields), null);

                // When
                manager.Rediscovery();

                // Then
                manager.Readers.Count().Should().Be(readerCount);
                manager.Writers.Count().Should().Be(writerCount);
                manager.Routers.Count().Should().Be(routerCount);
                manager.ExpireAfterSeconds = 9223372036854775807;
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

                var mock = new MockedMessagingClient(pairs);
                var conn = SocketConnectionTests.NewSocketConnection(mock.Client);
                conn.Init();

                var manager = new ClusterDiscoveryManager(conn, null);

                // When
                var exception = Record.Exception(()=>manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ServiceUnavailableException>();
                exception.Message.Should().StartWith("Error when calling `getServers` procedure: ");
            }

            [Fact]
            public void ShouldProtocolErrorWhenNoRecord()
            {
                // Given

                var manager = new ClusterDiscoveryManager(SetupConnection(new List<object[]>()), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Error when parsing `getServers` result: Sequence contains no elements.");
            }

            [Fact]
            public void ShouldProtocolErrorWhenMultipleRecord()
            {
                // Given
                var manager = new ClusterDiscoveryManager(SetupConnection(new List<object[]>
                {
                    CreateGetServersResponseRecordFields(3,2,1),
                    CreateGetServersResponseRecordFields(3,2,1)
                }), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Error when parsing `getServers` result: Sequence contains more than one element.");
            }

            [Fact]
            public void ShouldProtocolErrorWhenRecordUnparsable()
            {
                // Given
                var manager = new ClusterDiscoveryManager(SetupConnection(new object[] {1}), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Be("Error when parsing `getServers` result: keys (2) did not equal values (1).");
            }

            [Fact]
            public void ShouldThrowExceptionIfRouterIsEmpty()
            {
                // Given
                var recordFields = CreateGetServersResponseRecordFields(0,2,1);
                var manager = new ClusterDiscoveryManager(SetupConnection(recordFields), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(1);
                manager.Writers.Count().Should().Be(2);
                manager.Routers.Count().Should().Be(0);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("0 routers, 2 writers and 1 readers.");
            }

            [Fact]
            public void ShouldThrowExceptionIfReaderIsEmpty()
            {
                // Given
                var procedureReplyRecordFields = CreateGetServersResponseRecordFields(3,1,0);
                var manager = new ClusterDiscoveryManager(SetupConnection(procedureReplyRecordFields), null);

                // When
                var exception = Record.Exception(() => manager.Rediscovery());

                // Then
                manager.Readers.Count().Should().Be(0);
                manager.Writers.Count().Should().Be(1);
                manager.Routers.Count().Should().Be(3);
                exception.Should().BeOfType<ProtocolException>();
                exception.Message.Should().Contain("3 routers, 1 writers and 0 readers.");
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

        internal static IConnection SetupConnection(object[] recordFileds)
        {
            return SetupConnection(new List<object[]> {recordFileds});
        }

        internal static IConnection SetupConnection(List<object[]> recordFieldsList)
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

            var mock = new MockedMessagingClient(pairs);
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
