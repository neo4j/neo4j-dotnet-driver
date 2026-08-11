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
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class AuthTokenManagerFlowTests
{
    [Fact]
    public async Task The_registered_manager_requests_a_GetAuth_callback_for_its_token()
    {
        IAuthTokenManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new Stored<IAuthTokenManager>("manager-1", manager);
                });

        ICorrelatedRequest? capturedRequest = null;
        var roundTripMock = new Mock<IReverseRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<IAuthToken>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(new AuthorizationToken("basic", "neo4j", "pass").ToAuthToken());

        var newManagerHandler = new NewAuthTokenManagerHandler(
            objectStoreMock.Object,
            roundTripMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var tokenValue = await manager!.GetTokenAsync(TestContext.Current.CancellationToken);
        tokenValue.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)tokenValue;

        var request = capturedRequest.Should().BeOfType<AuthTokenManagerGetAuthRequest>().Subject;
        request.AuthTokenManagerId.Should().Be("manager-1");

        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }

    [Fact]
    public async Task The_registered_manager_requests_a_HandleSecurityException_callback()
    {
        IAuthTokenManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new Stored<IAuthTokenManager>("manager-1", manager);
                });

        ICorrelatedRequest? capturedRequest = null;
        var roundTripMock = new Mock<IReverseRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(true);

        var newManagerHandler = new NewAuthTokenManagerHandler(
            objectStoreMock.Object,
            roundTripMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var token = AuthTokens.Custom("neo4j", "pass", null!, "basic");
        var exception = new SecurityException("Neo.ClientError.Security.TokenExpired", "boom");

        var handled = await manager!.HandleSecurityExceptionAsync(
            token,
            exception,
            TestContext.Current.CancellationToken);

        handled.Should().BeTrue();

        var request = capturedRequest.Should().BeOfType<AuthTokenManagerHandleSecurityExceptionRequest>().Subject;
        request.AuthTokenManagerId.Should().Be("manager-1");
        request.ErrorCode.Should().Be("Neo.ClientError.Security.TokenExpired");
        request.Auth.Should().Be(new AuthorizationToken("basic", "neo4j", "pass"));
    }

    [Fact]
    public async Task
        The_registered_manager_requests_a_HandleSecurityException_callback_for_a_bearer_token_with_no_principal()
    {
        IAuthTokenManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new Stored<IAuthTokenManager>("manager-1", manager);
                });

        ICorrelatedRequest? capturedRequest = null;
        var roundTripMock = new Mock<IReverseRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(true);

        var newManagerHandler = new NewAuthTokenManagerHandler(
            objectStoreMock.Object,
            roundTripMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var token = AuthTokens.Bearer("token-value");
        var exception = new SecurityException("Neo.ClientError.Security.TokenExpired", "boom");

        var handled = await manager!.HandleSecurityExceptionAsync(
            token,
            exception,
            TestContext.Current.CancellationToken);

        handled.Should().BeTrue();

        var request = capturedRequest.Should().BeOfType<AuthTokenManagerHandleSecurityExceptionRequest>().Subject;
        request.Auth.Value.Scheme.Should().Be("bearer");
        request.Auth.Value.Credentials.Should().Be("token-value");
        request.Auth.Value.Principal.Should().BeNull();
    }

    [Fact]
    public async Task
        The_registered_manager_requests_a_HandleSecurityException_callback_for_a_custom_token_with_parameters()
    {
        IAuthTokenManager? manager = null;
        var objectStoreMock = new Mock<IObjectStore>();
        objectStoreMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new Stored<IAuthTokenManager>("manager-1", manager);
                });

        ICorrelatedRequest? capturedRequest = null;
        var roundTripMock = new Mock<IReverseRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<bool>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(true);

        var newManagerHandler = new NewAuthTokenManagerHandler(
            objectStoreMock.Object,
            roundTripMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var parameters = new Dictionary<string, object> { ["region"] = "eu-west" };
        var token = AuthTokens.Custom("neo4j", "pass", "realm1", "custom-scheme", parameters);
        var exception = new SecurityException("Neo.ClientError.Security.TokenExpired", "boom");

        var handled = await manager!.HandleSecurityExceptionAsync(
            token,
            exception,
            TestContext.Current.CancellationToken);

        handled.Should().BeTrue();

        var request = capturedRequest.Should().BeOfType<AuthTokenManagerHandleSecurityExceptionRequest>().Subject;
        request.Auth.Value.Scheme.Should().Be("custom-scheme");
        request.Auth.Value.Principal.Should().Be("neo4j");
        request.Auth.Value.Credentials.Should().Be("pass");
        request.Auth.Value.Realm.Should().Be("realm1");
        request.Auth.Value.Parameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void AuthTokenManagerGetAuthCompleted_fulfils_the_expectation_with_the_converted_token()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        IAuthToken? fulfilledToken = null;
        expectationsMock
            .Setup(e => e.Fulfil("callback-1", It.IsAny<IAuthToken>()))
            .Callback<string, IAuthToken>((_, token) => fulfilledToken = token);

        var handler = new AuthTokenManagerGetAuthCompletedHandler(expectationsMock.Object);
        var message = new AuthTokenManagerGetAuthCompleted
        {
            RequestId = "callback-1",
            Auth = new AuthorizationToken("basic", "neo4j", "pass")
        };

        handler.ProcessAsync(message);

        fulfilledToken.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)fulfilledToken!;
        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }

    [Fact]
    public void AuthTokenManagerHandleSecurityExceptionCompleted_fulfils_the_expectation_with_the_Handled_flag()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new AuthTokenManagerHandleSecurityExceptionCompletedHandler(expectationsMock.Object);
        var message = new AuthTokenManagerHandleSecurityExceptionCompleted { RequestId = "callback-1", Handled = true };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("callback-1", true), Times.Once);
    }
}
