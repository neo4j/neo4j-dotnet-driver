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
            .Setup(r => r.Register(It.IsAny<IAuthTokenManager>()))
            .Returns<IAuthTokenManager>(
                m =>
                {
                    manager = m;
                    return new RegistryObject<IAuthTokenManager>("manager-1", m);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchange>();
        callbacksMock
            .Setup(c => c.SendAsync<AuthTokenManagerGetAuthCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerGetAuthCompletedRequest
                {
                    RequestId = "callback-1",
                    Auth = new AuthorizationToken("basic", "neo4j", "pass")
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
            (getAuth, handle) => new TestKitAuthTokenManager(getAuth, handle),
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        Assert.NotNull(manager);

        var token = Assert.IsAssignableFrom<AuthToken>(
            await manager!.GetTokenAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(capturedRequest);
        var request = Assert.IsType<AuthTokenManagerGetAuthRequest>(capturedRequest!("callback-1"));
        Assert.Equal("manager-1", request.AuthTokenManagerId);

        Assert.Equal("basic", token.Content["scheme"]);
        Assert.Equal("neo4j", token.Content["principal"]);
        Assert.Equal("pass", token.Content["credentials"]);
    }

    [Fact]
    public async Task The_registered_manager_requests_a_HandleSecurityException_callback()
    {
        IAuthTokenManager? manager = null;
        var registryMock = new Mock<IRegistry>();
        registryMock
            .Setup(r => r.Register(It.IsAny<IAuthTokenManager>()))
            .Returns<IAuthTokenManager>(
                m =>
                {
                    manager = m;
                    return new RegistryObject<IAuthTokenManager>("manager-1", m);
                });

        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchange>();
        callbacksMock
            .Setup(
                c => c.SendAsync<AuthTokenManagerHandleSecurityExceptionCompletedRequest>(
                    It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new AuthTokenManagerHandleSecurityExceptionCompletedRequest
                {
                    RequestId = "callback-1",
                    Handled = true
                });

        var newManagerHandler = new NewAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
            (getAuth, handle) => new TestKitAuthTokenManager(getAuth, handle),
            Mock.Of<IResponseWriter>(),
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewAuthTokenManagerRequest());
        Assert.NotNull(manager);

        var token = AuthTokens.Custom("neo4j", "pass", null!, "basic");
        var exception = new SecurityException("Neo.ClientError.Security.TokenExpired", "boom");

        var handled = await manager!.HandleSecurityExceptionAsync(
            token,
            exception,
            TestContext.Current.CancellationToken);

        Assert.True(handled);

        Assert.NotNull(capturedRequest);
        var request = Assert.IsType<AuthTokenManagerHandleSecurityExceptionRequest>(capturedRequest!("callback-1"));
        Assert.Equal("manager-1", request.AuthTokenManagerId);
        Assert.Equal("Neo.ClientError.Security.TokenExpired", request.ErrorCode);
        Assert.Equal(new AuthorizationToken("basic", "neo4j", "pass"), request.Auth);
    }
}
