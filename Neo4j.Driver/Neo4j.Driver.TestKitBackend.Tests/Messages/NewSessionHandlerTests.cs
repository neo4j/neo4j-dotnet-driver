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
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class NewSessionHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<NewSessionHandler>();

    [Fact]
    public async Task Creates_a_session_on_the_driver_and_responds_with_its_id()
    {
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        var driverMock = _autoMocker.GetMock<IDriver>();
        driverMock.Setup(d => d.AsyncSession(It.IsAny<Action<SessionConfigBuilder>>())).Returns(sessionMock.Object);

        var storedSession = new Stored<IAsyncSession>("session-1", sessionMock.Object);
        _autoMocker.GetMock<IObjectStore>().Setup(r => r.Store(sessionMock.Object)).Returns(storedSession);

        var handler = _autoMocker.CreateInstance<NewSessionHandler>();
        var request = new NewSessionRequest
        {
            Driver = new Stored<IDriver>("driver-1", driverMock.Object),
            AccessMode = "r"
        };

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new SessionResponse("session-1")), Times.Once);
    }
}
