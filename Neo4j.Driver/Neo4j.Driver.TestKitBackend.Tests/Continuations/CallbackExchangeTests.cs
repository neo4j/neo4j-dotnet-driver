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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Continuations;

public class CallbackExchangerTests
{
    private record FakeRequest(string Id) : ICallbackRequest;

    private record FakeCompletedRequest : ICallbackResponse
    {
        public required string RequestId { get; init; }
        public string Tag { get; init; } = "";
    }

    private record OtherCompletedRequest : ICallbackResponse
    {
        public required string RequestId { get; init; }
    }

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<CallbackExchanger>();

    [Fact]
    public async Task Writes_the_request_then_returns_the_matching_completion()
    {
        FakeRequest? written = null;
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(m => written = (FakeRequest)m)
            .Returns(Task.CompletedTask);

        _autoMocker.GetMock<IConnectionInput>().Setup(i => i.ReadRequestAsync()).ReturnsAsync("completion-json");

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize("completion-json"))
            .Returns(() => new FakeCompletedRequest { RequestId = written!.Id, Tag = "hello" });

        var exchange = _autoMocker.CreateInstance<CallbackExchanger>();

        var response = await exchange.SendAsync<FakeCompletedRequest>(id => new FakeRequest(id));

        written.Should().NotBeNull();
        response.Tag.Should().Be("hello");
        response.RequestId.Should().Be(written!.Id);
    }

    [Fact]
    public async Task Throws_when_the_completion_is_the_wrong_type()
    {
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Returns(Task.CompletedTask);

        _autoMocker.GetMock<IConnectionInput>().Setup(i => i.ReadRequestAsync()).ReturnsAsync("completion-json");

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize("completion-json"))
            .Returns<string>(_ => new OtherCompletedRequest { RequestId = "whatever" });

        var exchange = _autoMocker.CreateInstance<CallbackExchanger>();

        Func<Task> act = () => exchange.SendAsync<FakeCompletedRequest>(id => new FakeRequest(id));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Throws_when_the_completion_request_id_does_not_match()
    {
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Returns(Task.CompletedTask);

        _autoMocker.GetMock<IConnectionInput>().Setup(i => i.ReadRequestAsync()).ReturnsAsync("completion-json");

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize("completion-json"))
            .Returns<string>(_ => new FakeCompletedRequest { RequestId = "not-the-right-id" });

        var exchange = _autoMocker.CreateInstance<CallbackExchanger>();

        Func<Task> act = () => exchange.SendAsync<FakeCompletedRequest>(id => new FakeRequest(id));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Throws_when_the_connection_closes_before_a_completion_arrives()
    {
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Returns(Task.CompletedTask);

        _autoMocker.GetMock<IConnectionInput>().Setup(i => i.ReadRequestAsync()).ReturnsAsync((string?)null);

        var exchange = _autoMocker.CreateInstance<CallbackExchanger>();

        Func<Task> act = () => exchange.SendAsync<FakeCompletedRequest>(id => new FakeRequest(id));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
