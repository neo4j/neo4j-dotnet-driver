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
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class NewSessionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<NewSessionHandler>();

    [Fact]
    public async Task Creates_a_session_on_the_driver_and_responds_with_its_id()
    {
        var registry = new Registry();
        _autoMocker.Use<IRegistry>(registry);

        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        var driverMock = _autoMocker.GetMock<IDriver>();
        driverMock.Setup(d => d.AsyncSession()).Returns(sessionMock.Object);
        var registeredDriver = registry.Register(driverMock.Object);

        var handler = _autoMocker.CreateInstance<NewSessionHandler>();
        var request = new NewSessionRequest { Driver = registeredDriver };

        var response = await handler.ProcessAsync(request);

        var sessionResponse = response.Should().BeOfType<SessionResponse>().Subject;
        registry.Get<IAsyncSession>(sessionResponse.Id).Object.Should().Be(sessionMock.Object);
    }
}
