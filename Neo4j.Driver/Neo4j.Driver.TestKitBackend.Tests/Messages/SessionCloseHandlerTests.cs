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
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class SessionCloseHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SessionCloseHandler>();

    [Fact]
    public async Task Closes_the_session_removes_it_from_the_registry_and_responds_with_its_id()
    {
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        var registered = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Get<IAsyncSession>("session-1")).Returns(registered);

        var handler = _autoMocker.CreateInstance<SessionCloseHandler>();
        var request = new SessionCloseRequest { Session = registered };

        var response = await handler.ProcessAsync(request);

        sessionMock.Verify(s => s.CloseAsync(), Times.Once);
        _autoMocker.GetMock<IRegistry>().Verify(r => r.Remove("session-1"), Times.Once);
        response.Should().BeOfType<SessionResponse>().Subject.Id.Should().Be(registered.Id);
    }
}
