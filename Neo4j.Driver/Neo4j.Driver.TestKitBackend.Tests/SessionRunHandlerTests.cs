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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class SessionRunHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SessionRunHandler>();

    [Fact]
    public async Task Runs_the_query_and_responds_with_the_cursor_id_and_keys()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.KeysAsync()).ReturnsAsync(["n"]);

        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock.Setup(s => s.RunAsync("RETURN 1 AS n")).ReturnsAsync(cursorMock.Object);

        var registeredCursor = new RegistryObject<IResultCursor>("result-1", cursorMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(cursorMock.Object)).Returns(registeredCursor);

        var handler = _autoMocker.CreateInstance<SessionRunHandler>();
        var request = new SessionRunRequest
        {
            Session = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object),
            Cypher = "RETURN 1 AS n"
        };

        var response = await handler.ProcessAsync(request);

        var resultResponse = response.Should().BeOfType<ResultResponse>().Subject;
        resultResponse.Id.Should().Be("result-1");
        resultResponse.Keys.Should().Equal("n");
    }
}
