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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class BasicAuthTokenManagerFlowTests
{
    private record TerminalResponse(string Tag) : IProtocolMessage;

    [Fact]
    public async Task The_registered_manager_round_trips_a_provider_callback_for_its_token()
    {
        var coordinator = new ContinuationCoordinator();
        var responseWriterMock = new Mock<IResponseWriter>();

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

        var newManagerHandler = new NewBasicAuthTokenManagerHandler(
            registryMock.Object,
            coordinator,
            responseWriterMock.Object,
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewBasicAuthTokenManagerRequest());

        responseWriterMock.Verify(w => w.WriteAsync(new BasicAuthTokenManagerResponse("manager-1")), Times.Once);
        Assert.NotNull(manager);

        var openRequestTask = coordinator.WaitForNextResponseAsync();

        var tokenTask = manager!.GetTokenAsync(TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<BasicAuthTokenProviderRequest>(await WithTimeoutAsync(openRequestTask));
        Assert.Equal("manager-1", callbackRequest.BasicAuthTokenManagerId);

        var completedHandler = new CallbackCompletedHandler<BasicAuthTokenProviderCompletedRequest>(
            coordinator,
            responseWriterMock.Object);
        var completedTask = completedHandler.ProcessAsync(
            new BasicAuthTokenProviderCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Auth = new AuthorizationToken("basic", "neo4j", "pass")
            });

        var token = Assert.IsAssignableFrom<AuthToken>(await WithTimeoutAsync(tokenTask.AsTask()));
        Assert.Equal("basic", token.Content["scheme"]);
        Assert.Equal("neo4j", token.Content["principal"]);
        Assert.Equal("pass", token.Content["credentials"]);

        coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
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
