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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BasicAuthTokenManagerFlowTests
{
    [Fact]
    public async Task The_stored_manager_requests_a_provider_callback_for_its_token()
    {
        IAuthTokenManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Store(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return "manager-1";
                });

        IProtocolMessage? capturedRequest = null;
        var roundTripMock = new Mock<IOutboundRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<IAuthToken>(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(request => capturedRequest = request)
            .ReturnsAsync(new AuthorizationToken
            {
                Scheme = "basic",
                Principal = "neo4j",
                Credentials = "pass"
            }.ToAuthToken());

        var newManagerHandler = new NewBasicAuthTokenManagerHandler(
            objectStoreMock.Object,
            roundTripMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewBasicAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var tokenValue = await manager!.GetTokenAsync(TestContext.Current.CancellationToken);
        tokenValue.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)tokenValue;

        var request = capturedRequest.Should().BeOfType<BasicAuthTokenProviderRequest>().Subject;
        request.BasicAuthTokenManagerId.Should().Be("manager-1");

        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }

    [Fact]
    public void BasicAuthTokenProviderCompleted_fulfils_the_expectation_with_the_converted_token()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        IAuthToken? fulfilledToken = null;
        expectationsMock
            .Setup(e => e.Fulfil("callback-1", It.IsAny<IAuthToken>()))
            .Callback<string, IAuthToken>((_, token) => fulfilledToken = token);

        var handler = new BasicAuthTokenProviderCompletedHandler(expectationsMock.Object);
        var message = new BasicAuthTokenProviderCompleted
        {
            RequestId = "callback-1",
            Auth = new AuthorizationToken { Scheme = "basic", Principal = "neo4j", Credentials = "pass" }
        };

        handler.ProcessAsync(message);

        fulfilledToken.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)fulfilledToken!;
        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }
}
