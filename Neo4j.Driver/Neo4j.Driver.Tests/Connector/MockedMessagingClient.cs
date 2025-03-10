// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Tests.Connector;

/// <summary>
/// If you want to create a connection with full control of what messages to send and what messages to receive,
/// you will find this mocked client very useful. This client provides a clear cut from encoding/decoding messages and
/// writing/reading from a real socket. Instead, you talk messages directly. When creating a mocked client, you provide
/// what you expect this client to send and what you want the server to response. When sending a request message via this
/// client, it verifies that the message to send is the same as expected. Then when the driver wants to consume a response
/// from the server, the response message specified at the initialization will be replied in order.
/// </summary>
internal class MockedMessagingClientV3
{
    private readonly IList<IRequestMessage> _requestMessages = new List<IRequestMessage>();
    private readonly IList<IResponseMessage> _responseMessages = new List<IResponseMessage>();
    private int _requestCount;
    private int _responseCount;

    /// <summary>Create a mocked client for testing</summary>
    /// <param name="requestAndResponsePairs">
    /// The is no one-to-one mapping in the request and response pair. Only the order of
    /// request messages (and/or response messages) that really matters. However it is suggested to have client request message
    /// and the expected server response message matched in a pair to make it easier to read in code. In case of no matched
    /// value for a request (or receive) message, use <c>null</c> as a place holder.
    /// </param>
    /// <param name="clientMock">Set this parameter if you want to pass in the mocked client yourself.</param>
    public MockedMessagingClientV3(
        IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs,
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
        ClientMock.Setup(x => x.ConnectAsync(CancellationToken.None))
            .Returns(Task.FromResult(new Mock<IBoltProtocol>().Object));

        ClientMock.Setup(x => x.SendAsync(It.IsAny<IEnumerable<IRequestMessage>>()))
            .Callback<IEnumerable<IRequestMessage>>(
                msg =>
                {
                    foreach (var m in msg)
                    {
                        m.ToString().Should().Be(_requestMessages[_requestCount].ToString());
                        _requestCount++;
                    }
                });

        ClientMock.Setup(x => x.ReceiveOneAsync(It.IsAny<IResponsePipeline>()))
            .Callback<IResponsePipeline>(
                pipeline =>
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

    public IList<IRequestMessage> SendMessages => new List<IRequestMessage>(_requestMessages);
    public IList<IResponseMessage> ReceiveMessages => new List<IResponseMessage>(_responseMessages);

    public Mock<ISocketClient> ClientMock { get; }
    public ISocketClient Client => ClientMock.Object;

    internal static HelloMessage LoginMessage(IAuthToken auth = null)
    {
        auth ??= AuthTokens.None;
        return new HelloMessage(
            BoltProtocolVersion.V3_0,
            Config.DefaultUserAgent,
            auth.AsDictionary(),
            null);
    }

    internal static SuccessMessage SuccessMessage(IList<object> fields = null)
    {
        return fields == null
            ? new SuccessMessage(new Dictionary<string, object>())
            : new SuccessMessage(new Dictionary<string, object> { { "fields", fields } });
    }

    internal static Tuple<IRequestMessage, IResponseMessage> MessagePair(
        IRequestMessage request,
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
    public MockedMessagingClientV4_1(
        IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs,
        Mock<ISocketClient> clientMock = null)
        : base(requestAndResponsePairs, clientMock)
    {
    }

    internal new static HelloMessage LoginMessage(IAuthToken auth = null)
    {
        auth ??= AuthTokens.None;
        return new HelloMessage(
            BoltProtocolVersion.V4_1,
            Config.DefaultUserAgent,
            auth.AsDictionary(),
            null);
    }
}

internal class MockedMessagingClientV4_3 : MockedMessagingClientV4_1
{
    public MockedMessagingClientV4_3(
        IList<Tuple<IRequestMessage, IResponseMessage>> requestAndResponsePairs,
        Mock<ISocketClient> clientMock = null)
        : base(requestAndResponsePairs, clientMock)
    {
    }

    internal static SuccessMessage SuccessMessage(IDictionary<string, object> fields = null)
    {
        return fields == null
            ? new SuccessMessage(new Dictionary<string, object>())
            : new SuccessMessage(fields);
    }
}
