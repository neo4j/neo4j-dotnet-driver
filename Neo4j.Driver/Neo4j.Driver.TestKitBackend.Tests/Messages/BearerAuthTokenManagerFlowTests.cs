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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

using WireAuthTokenAndExpiration = Neo4j.Driver.TestKitBackend.Messages.AuthTokenAndExpiration;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BearerAuthTokenManagerFlowTests
{
    private record TerminalResponse(string Tag) : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
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
            _coordinator,
            _responseWriterMock.Object,
            Mock.Of<ILogger>());

        newManagerHandler.ProcessAsync(new NewBearerAuthTokenManagerRequest()).GetAwaiter().GetResult();

        Assert.NotNull(manager);
        return manager!;
    }

    [Fact]
    public async Task The_registered_manager_round_trips_a_provider_callback_for_its_token()
    {
        var manager = RegisterManager();

        _responseWriterMock.Verify(w => w.WriteAsync(new BearerAuthTokenManagerResponse("manager-1")), Times.Once);

        var openRequestTask = _coordinator.WaitForNextResponseAsync();

        var tokenTask = manager.GetTokenAsync(TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BearerAuthTokenProviderRequest>(await WithTimeoutAsync(openRequestTask));
        Assert.Equal("manager-1", callbackRequest.BearerAuthTokenManagerId);

        var completedHandler = new CallbackCompletedHandler<BearerAuthTokenProviderCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BearerAuthTokenProviderCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Auth = new WireAuthTokenAndExpiration(
                    new AuthorizationToken("bearer", "", "a-token"),
                    ExpiresInMs: 60_000)
            });

        var token = Assert.IsAssignableFrom<AuthToken>(await WithTimeoutAsync(tokenTask.AsTask()));
        Assert.Equal("bearer", token.Content["scheme"]);
        Assert.Equal("a-token", token.Content["credentials"]);

        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
    }

    [Fact]
    public async Task A_token_without_expiry_never_expires_so_the_provider_is_not_called_again()
    {
        var manager = RegisterManager();

        var openRequestTask = _coordinator.WaitForNextResponseAsync();
        var tokenTask = manager.GetTokenAsync(TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BearerAuthTokenProviderRequest>(await WithTimeoutAsync(openRequestTask));

        var completedHandler = new CallbackCompletedHandler<BearerAuthTokenProviderCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BearerAuthTokenProviderCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Auth = new WireAuthTokenAndExpiration(new AuthorizationToken("bearer", "", "a-token"))
            });

        await WithTimeoutAsync(tokenTask.AsTask());
        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        var secondToken = Assert.IsAssignableFrom<AuthToken>(
            await WithTimeoutAsync(manager.GetTokenAsync(TestContext.Current.CancellationToken).AsTask()));

        Assert.Equal("a-token", secondToken.Content["credentials"]);
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

            await RoundTripTokenAsync(manager, "first-token", expiresInMs: 10_000);

            fakeTime.Tick(10_001);

            var refreshed = await RoundTripTokenAsync(manager, "second-token", expiresInMs: 10_000);
            Assert.Equal("second-token", refreshed.Content["credentials"]);
        }
        finally
        {
            DateTimeProvider.StaticInstance = original;
        }
    }

    private async Task<AuthToken> RoundTripTokenAsync(IAuthTokenManager manager, string credentials, long expiresInMs)
    {
        var openRequestTask = _coordinator.WaitForNextResponseAsync();
        var tokenTask = manager.GetTokenAsync(TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BearerAuthTokenProviderRequest>(await WithTimeoutAsync(openRequestTask));

        var completedHandler = new CallbackCompletedHandler<BearerAuthTokenProviderCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BearerAuthTokenProviderCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Auth = new WireAuthTokenAndExpiration(
                    new AuthorizationToken("bearer", "", credentials),
                    expiresInMs)
            });

        var token = Assert.IsAssignableFrom<AuthToken>(await WithTimeoutAsync(tokenTask.AsTask()));

        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        return token;
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        return await task;
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        await task;
    }
}
