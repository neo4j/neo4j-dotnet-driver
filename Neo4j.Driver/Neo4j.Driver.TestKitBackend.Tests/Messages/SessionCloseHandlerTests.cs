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

public class SessionCloseHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SessionCloseHandler>();

    [Fact]
    public async Task Closes_the_session_removes_it_from_the_objectStore_and_responds_with_its_id()
    {
        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        _autoMocker.GetMock<IObjectStore>().Setup(r => r.Get<IAsyncSession>("session-1")).Returns(sessionMock.Object);

        var handler = _autoMocker.CreateInstance<SessionCloseHandler>();
        var request = new SessionCloseRequest { Session = sessionMock.Object, SessionId = "session-1" };

        await handler.ProcessAsync(request);

        sessionMock.Verify(s => s.CloseAsync(), Times.Once);
        _autoMocker.GetMock<IObjectStore>().Verify(r => r.Remove("session-1"), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new SessionResponse("session-1")), Times.Once);
    }
}
