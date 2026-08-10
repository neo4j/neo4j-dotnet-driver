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
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class AuthTokenManagerFlowTests
{
    [Fact]
    public async Task The_registered_manager_requests_a_GetAuth_callback_for_its_token()
    {
        IAuthTokenManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new RegistryObject<IAuthTokenManager>("manager-1", manager);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchanger>();
        callbacksMock
            .Setup(c => c.SendAsync<AuthTokenManagerGetAuthCompleted>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerGetAuthCompleted
                {
                    RequestId = "callback-1",
                    Auth = new AuthorizationToken("basic", "neo4j", "pass")
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        manager.Should().NotBeNull();

        var tokenValue = await manager!.GetTokenAsync(TestContext.Current.CancellationToken);
        tokenValue.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)tokenValue;

        capturedRequest.Should().NotBeNull();
        var request = capturedRequest!("callback-1");
        request.Should().BeOfType<AuthTokenManagerGetAuthRequest>();
        ((AuthTokenManagerGetAuthRequest)request).AuthTokenManagerId.Should().Be("manager-1");

        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }

    [Fact]
    public async Task The_registered_manager_requests_a_HandleSecurityException_callback()
    {
        IAuthTokenManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new RegistryObject<IAuthTokenManager>("manager-1", manager);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchanger>();
        callbacksMock
            .Setup(
                c => c.SendAsync<AuthTokenManagerHandleSecurityExceptionCompleted>(
                    It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerHandleSecurityExceptionCompleted
                {
                    RequestId = "callback-1",
                    Handled = true
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
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

        capturedRequest.Should().NotBeNull();
        var request = capturedRequest!("callback-1");
        request.Should().BeOfType<AuthTokenManagerHandleSecurityExceptionRequest>();
        var securityExceptionRequest = (AuthTokenManagerHandleSecurityExceptionRequest)request;
        securityExceptionRequest.AuthTokenManagerId.Should().Be("manager-1");
        securityExceptionRequest.ErrorCode.Should().Be("Neo.ClientError.Security.TokenExpired");
        securityExceptionRequest.Auth.Should().Be(new AuthorizationToken("basic", "neo4j", "pass"));
    }

    [Fact]
    public async Task
        The_registered_manager_requests_a_HandleSecurityException_callback_for_a_bearer_token_with_no_principal()
    {
        IAuthTokenManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new RegistryObject<IAuthTokenManager>("manager-1", manager);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchanger>();
        callbacksMock
            .Setup(
                c => c.SendAsync<AuthTokenManagerHandleSecurityExceptionCompleted>(
                    It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerHandleSecurityExceptionCompleted
                {
                    RequestId = "callback-1",
                    Handled = true
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
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

        capturedRequest.Should().NotBeNull();
        var request = capturedRequest!("callback-1");
        var securityExceptionRequest = (AuthTokenManagerHandleSecurityExceptionRequest)request;
        securityExceptionRequest.Auth.Value.Scheme.Should().Be("bearer");
        securityExceptionRequest.Auth.Value.Credentials.Should().Be("token-value");
        securityExceptionRequest.Auth.Value.Principal.Should().BeNull();
    }

    [Fact]
    public async Task
        The_registered_manager_requests_a_HandleSecurityException_callback_for_a_custom_token_with_parameters()
    {
        IAuthTokenManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<Func<string, IAuthTokenManager>>()))
            .Returns<Func<string, IAuthTokenManager>>(
                create =>
                {
                    manager = create("manager-1");
                    return new RegistryObject<IAuthTokenManager>("manager-1", manager);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchanger>();
        callbacksMock
            .Setup(
                c => c.SendAsync<AuthTokenManagerHandleSecurityExceptionCompleted>(
                    It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerHandleSecurityExceptionCompleted
                {
                    RequestId = "callback-1",
                    Handled = true
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
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

        capturedRequest.Should().NotBeNull();
        var request = capturedRequest!("callback-1");
        var securityExceptionRequest = (AuthTokenManagerHandleSecurityExceptionRequest)request;
        securityExceptionRequest.Auth.Value.Scheme.Should().Be("custom-scheme");
        securityExceptionRequest.Auth.Value.Principal.Should().Be("neo4j");
        securityExceptionRequest.Auth.Value.Credentials.Should().Be("pass");
        securityExceptionRequest.Auth.Value.Realm.Should().Be("realm1");
        securityExceptionRequest.Auth.Value.Parameters.Should().BeEquivalentTo(parameters);
    }
}
