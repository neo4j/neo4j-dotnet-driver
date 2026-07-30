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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class VerifyAuthenticationHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<VerifyAuthenticationHandler>();

    [Fact]
    public async Task Returns_whether_the_authorization_token_is_valid()
    {
        var driverMock = _autoMocker.GetMock<IDriver>();
        driverMock.Setup(d => d.VerifyAuthenticationAsync(It.IsAny<IAuthToken>())).ReturnsAsync(true);
        var registered = new RegistryObject<IDriver>("driver-1", driverMock.Object);

        var handler = _autoMocker.CreateInstance<VerifyAuthenticationHandler>();
        var request = new VerifyAuthenticationRequest
        {
            Driver = registered,
            AuthorizationToken = new AuthorizationToken("basic", "neo4j", "secret")
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new DriverIsAuthenticatedResponse("driver-1", true)), Times.Once);
    }
}
