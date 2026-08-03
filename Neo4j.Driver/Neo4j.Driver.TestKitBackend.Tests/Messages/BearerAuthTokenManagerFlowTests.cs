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
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

using WireAuthTokenAndExpiration = Neo4j.Driver.TestKitBackend.Messages.AuthTokenAndExpiration;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BearerAuthTokenManagerFlowTests
{
    private readonly Mock<ICallbackExchanger> _callbacksMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IAuthTokenManager RegisterManager()
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

        var newManagerHandler = new NewBearerAuthTokenManagerHandler(
            registryMock.Object,
            _callbacksMock.Object,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        newManagerHandler.ProcessAsync(new NewBearerAuthTokenManagerRequest()).GetAwaiter().GetResult();

        Assert.NotNull(manager);
        return manager!;
    }

    private void SetupNextToken(string credentials, long? expiresInMs)
    {
        _callbacksMock
            .Setup(
                c => c.SendAsync<BearerAuthTokenProviderCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .ReturnsAsync(
                new BearerAuthTokenProviderCompletedRequest
                {
                    RequestId = "callback",
                    Auth = new WireAuthTokenAndExpiration(new AuthorizationToken("bearer", "", credentials), expiresInMs)
                });
    }

    [Fact]
    public async Task The_registered_manager_requests_a_provider_callback_for_its_token()
    {
        var manager = RegisterManager();

        _responseWriterMock.Verify(w => w.WriteAsync(new BearerAuthTokenManagerResponse("manager-1")), Times.Once);

        SetupNextToken("a-token", 60_000);

        var token = Assert.IsAssignableFrom<AuthToken>(
            await manager.GetTokenAsync(TestContext.Current.CancellationToken));

        Assert.Equal("bearer", token.Content["scheme"]);
        Assert.Equal("a-token", token.Content["credentials"]);
    }

    [Fact]
    public async Task A_token_without_expiry_never_expires_so_the_provider_is_not_called_again()
    {
        var manager = RegisterManager();
        SetupNextToken("a-token", expiresInMs: null);

        await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        _callbacksMock.Invocations.Clear();

        var secondToken = Assert.IsAssignableFrom<AuthToken>(
            await manager.GetTokenAsync(TestContext.Current.CancellationToken));

        Assert.Equal("a-token", secondToken.Content["credentials"]);
        _callbacksMock.Verify(
            c => c.SendAsync<BearerAuthTokenProviderCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()),
            Times.Never);
    }

    [Fact]
    public async Task An_expired_token_is_refreshed_when_fake_time_was_installed_after_the_manager()
    {
        var original = DateTimeProvider.StaticInstance;
        try
        {
            var manager = RegisterManager();

            var fakeTime = new FakeTimeService();
            fakeTime.Install();

            SetupNextToken("first-token", expiresInMs: 10_000);
            await manager.GetTokenAsync(TestContext.Current.CancellationToken);

            fakeTime.Tick(10_001);

            SetupNextToken("second-token", expiresInMs: 10_000);
            var refreshed = Assert.IsAssignableFrom<AuthToken>(
                await manager.GetTokenAsync(TestContext.Current.CancellationToken));

            Assert.Equal("second-token", refreshed.Content["credentials"]);
        }
        finally
        {
            DateTimeProvider.StaticInstance = original;
        }
    }
}
