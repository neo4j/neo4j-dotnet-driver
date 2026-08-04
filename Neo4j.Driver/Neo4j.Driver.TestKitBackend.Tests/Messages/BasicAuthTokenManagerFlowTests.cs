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

public class BasicAuthTokenManagerFlowTests
{
    [Fact]
    public async Task The_registered_manager_requests_a_provider_callback_for_its_token()
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
            .Setup(c => c.SendAsync<BasicAuthTokenProviderCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new BasicAuthTokenProviderCompletedRequest
                {
                    RequestId = "callback-1",
                    Auth = new AuthorizationToken("basic", "neo4j", "pass")
                });

        var responseWriterMock = new Mock<IResponseWriter>();
        var newManagerHandler = new NewBasicAuthTokenManagerHandler(
            registryMock.Object,
            callbacksMock.Object,
            responseWriterMock.Object,
            Mock.Of<ILogger>());

        await newManagerHandler.ProcessAsync(new NewBasicAuthTokenManagerRequest());

        responseWriterMock.Verify(w => w.WriteAsync(new BasicAuthTokenManagerResponse("manager-1")), Times.Once);
        manager.Should().NotBeNull();

        var tokenValue = await manager!.GetTokenAsync(TestContext.Current.CancellationToken);
        tokenValue.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)tokenValue;

        capturedRequest.Should().NotBeNull();
        var request = capturedRequest!("callback-1");
        request.Should().BeOfType<BasicAuthTokenProviderRequest>();
        ((BasicAuthTokenProviderRequest)request).BasicAuthTokenManagerId.Should().Be("manager-1");

        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }
}
