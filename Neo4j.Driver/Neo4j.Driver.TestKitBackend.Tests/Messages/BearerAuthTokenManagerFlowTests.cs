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
using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

using WireAuthTokenAndExpiration = Neo4j.Driver.TestKitBackend.Messages.AuthTokenAndExpiration;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

[Collection(FakeSystemClockCollection.Name)]
public class BearerAuthTokenManagerFlowTests
{
    private readonly Mock<ICallbackExchanger> _callbacksMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private IAuthTokenManager RegisterManager()
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

        var newManagerHandler = new NewBearerAuthTokenManagerHandler(
            objectStoreMock.Object,
            _callbacksMock.Object,
            new CurrentDateTimeProvider(),
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        newManagerHandler.ProcessAsync(new NewBearerAuthTokenManagerRequest()).GetAwaiter().GetResult();

        manager.Should().NotBeNull();
        return manager!;
    }

    private void SetupNextToken(string credentials, long? expiresInMs)
    {
        _callbacksMock
            .Setup(
                c => c.SendAsync<BearerAuthTokenProviderCompleted>(It.IsAny<Func<string, ICallbackRequest>>()))
            .ReturnsAsync(
                new BearerAuthTokenProviderCompleted
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

        var tokenValue = await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        tokenValue.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)tokenValue;

        token.Content["scheme"].Should().Be("bearer");
        token.Content["credentials"].Should().Be("a-token");
    }

    [Fact]
    public async Task A_token_without_expiry_never_expires_so_the_provider_is_not_called_again()
    {
        var manager = RegisterManager();
        SetupNextToken("a-token", expiresInMs: null);

        await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        _callbacksMock.Invocations.Clear();

        var secondTokenValue = await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        secondTokenValue.Should().BeAssignableTo<AuthToken>();
        var secondToken = (AuthToken)secondTokenValue;

        secondToken.Content["credentials"].Should().Be("a-token");
        _callbacksMock.Verify(
            c => c.SendAsync<BearerAuthTokenProviderCompleted>(It.IsAny<Func<string, ICallbackRequest>>()),
            Times.Never);
    }

    [Fact]
    public async Task A_token_with_expiry_beyond_int_MaxValue_milliseconds_stays_valid()
    {
        var manager = RegisterManager();
        SetupNextToken("a-token", expiresInMs: (long)int.MaxValue + 10_000);

        await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        _callbacksMock.Invocations.Clear();

        var secondTokenValue = await manager.GetTokenAsync(TestContext.Current.CancellationToken);
        secondTokenValue.Should().BeAssignableTo<AuthToken>();
        var secondToken = (AuthToken)secondTokenValue;

        secondToken.Content["credentials"].Should().Be("a-token");
        _callbacksMock.Verify(
            c => c.SendAsync<BearerAuthTokenProviderCompleted>(It.IsAny<Func<string, ICallbackRequest>>()),
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
            var refreshedValue = await manager.GetTokenAsync(TestContext.Current.CancellationToken);
            refreshedValue.Should().BeAssignableTo<AuthToken>();
            var refreshed = (AuthToken)refreshedValue;

            refreshed.Content["credentials"].Should().Be("second-token");
        }
        finally
        {
            DateTimeProvider.StaticInstance = original;
        }
    }
}
