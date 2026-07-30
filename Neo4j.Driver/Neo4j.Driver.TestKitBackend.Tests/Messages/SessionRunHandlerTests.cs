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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

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

        await handler.ProcessAsync(request);

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(It.Is<ResultResponse>(
                    r => r.Id == "result-1" && r.Keys!.SequenceEqual(new[] { "n" }))),
                Times.Once);
    }

    [Fact]
    public async Task Maps_params_and_runs_the_query_with_them()
    {
        var cursorMock = _autoMocker.GetMock<IResultCursor>();
        cursorMock.Setup(c => c.KeysAsync()).ReturnsAsync(["n"]);

        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(new CypherInt(1))).Returns(1L);

        var sessionMock = _autoMocker.GetMock<IAsyncSession>();
        sessionMock
            .Setup(s => s.RunAsync("RETURN $p AS n", It.Is<IDictionary<string, object>>(
                p => p.Count == 1 && Equals(p["p"], 1L))))
            .ReturnsAsync(cursorMock.Object);

        var registeredCursor = new RegistryObject<IResultCursor>("result-1", cursorMock.Object);
        _autoMocker.GetMock<IRegistry>().Setup(r => r.Register(cursorMock.Object)).Returns(registeredCursor);

        var handler = _autoMocker.CreateInstance<SessionRunHandler>();
        var request = new SessionRunRequest
        {
            Session = new RegistryObject<IAsyncSession>("session-1", sessionMock.Object),
            Cypher = "RETURN $p AS n",
            Params = new Dictionary<string, ICypherValue> { ["p"] = new CypherInt(1) }
        };

        await handler.ProcessAsync(request);

        sessionMock.Verify(
            s => s.RunAsync("RETURN $p AS n", It.Is<IDictionary<string, object>>(
                p => p.Count == 1 && Equals(p["p"], 1L))),
            Times.Once);
    }
}
