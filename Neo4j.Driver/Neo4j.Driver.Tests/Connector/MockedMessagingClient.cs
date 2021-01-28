﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using V3 = Neo4j.Driver.Internal.Messaging.V3;
using V4_1 = Neo4j.Driver.Internal.Messaging.V4_1;

namespace Neo4j.Driver.Tests.Routing
{
    /// <summary>
    /// If you want to create a connection with full control of what messages to send and what messages to receive,
    /// you will find this mocked client very useful.
    /// This client provides a clear cut from encoding/decoding messages and writing/reading from a real socket.
    /// Instead, you talk messages directly. 
    /// When creating a mocked client, you provide what you expect this client to send and what you want the server to response.
    /// When sending a request message via this client, it verifies that the message to send is the same as expected.
    /// Then when the driver wants to consume a response from the server, the response message specified at the initialization will be replied in order.
    /// </summary>
    internal class MockedMessagingClientV3
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
        public MockedMessagingClientV3(IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs,
            Mock<ISocketClient> clientMock = null)
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
            ClientMock.Setup(x => x.ConnectAsync(null)).Returns(Task.FromResult(new Mock<IBoltProtocol>().Object));
            ClientMock.Setup(x => x.SendAsync(It.IsAny<IEnumerable<IRequestMessage>>()))
                .Callback<IEnumerable<IRequestMessage>>(msg =>
                {
                    foreach (var m in msg)
                    {
                        m.ToString().Should().Be(_requestMessages[_requestCount].ToString());
                        _requestCount++;
                    }
                });

            ClientMock.Setup(x => x.ReceiveOneAsync(It.IsAny<IResponsePipeline>()))
                .Callback<IResponsePipeline>(pipeline =>
                {
                    if (_responseCount < _responseMessages.Count)
                    {
                         _responseMessages[_responseCount].Dispatch(pipeline);
                        _responseCount++;
                    }
                    else
                    {
                        throw new InvalidOperationException("Not enough response message to provide");
                    }

                });
            ClientMock.Setup(x => x.IsOpen).Returns(() => _responseCount < _responseMessages.Count);
        }

        internal static V3.HelloMessage LoginMessage(IAuthToken auth = null)
        {
            auth = auth ?? AuthTokens.None;
            return new V3.HelloMessage(ConnectionSettings.DefaultUserAgent, auth.AsDictionary());
        }

        internal static SuccessMessage SuccessMessage(IList<object> fields = null)
        {
            return fields == null
                ? new SuccessMessage(new Dictionary<string, object>())
                : new SuccessMessage(new Dictionary<string, object> {{"fields", fields}});
        }

        internal static Tuple<IRequestMessage, IResponseMessage> MessagePair(IRequestMessage request,
            IResponseMessage response)
        {
            return new Tuple<IRequestMessage, IResponseMessage>(request, response);
        }

        internal static Tuple<IRequestMessage, IResponseMessage> MessagePair(IResponseMessage response)
        {
            return MessagePair(null, response);
        }
    }



    internal class MockedMessagingClientV4_1 : MockedMessagingClientV3
    {
        public MockedMessagingClientV4_1(IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs, Mock<ISocketClient> clientMock = null)
            : base(requestAndResponsePairs, clientMock)
        {
        }

        internal new static V4_1.HelloMessage LoginMessage(IAuthToken auth = null)
        {
            auth = auth ?? AuthTokens.None;
            return new V4_1.HelloMessage(ConnectionSettings.DefaultUserAgent, auth.AsDictionary(), null);
        }
    }
}
