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

using Autofac.Features.Indexed;
using FluentAssertions;
using Moq;
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class MessageDispatcherTests
{
    private readonly Mock<IIndex<Type, IMessageHandler>> _handlers = new();
    private readonly Mock<IResponseWriter> _writer = new();

    private MessageDispatcher Subject() => new(_handlers.Object, _writer.Object);

    [Fact]
    public async Task Dispatches_to_the_handler_keyed_by_message_type_and_writes_its_response()
    {
        var request = new SampleRequest();
        var response = new SampleResponse();

        var handler = new Mock<IMessageHandler>();
        handler.Setup(h => h.ProcessAsync(request)).ReturnsAsync(response);

        IMessageHandler resolved = handler.Object;
        _handlers.Setup(h => h.TryGetValue(typeof(SampleRequest), out resolved!)).Returns(true);

        await Subject().DispatchAsync(request);

        _writer.Verify(w => w.WriteAsync(response), Times.Once);
    }

    [Fact]
    public async Task Throws_when_no_handler_is_registered_for_the_message_type()
    {
        IMessageHandler resolved = null!;
        _handlers.Setup(h => h.TryGetValue(It.IsAny<Type>(), out resolved!)).Returns(false);

        var dispatch = async () => await Subject().DispatchAsync(new SampleRequest());

        await dispatch.Should().ThrowAsync<InvalidOperationException>();
    }

    private record SampleRequest : IProtocolMessage;

    private record SampleResponse : IProtocolMessage;
}
