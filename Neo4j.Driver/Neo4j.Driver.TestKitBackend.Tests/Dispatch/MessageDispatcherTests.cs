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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Dispatch;

public class MessageDispatcherTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<MessageDispatcher>();

    private MessageDispatcher Subject(params Func<IMessageHandler>[] handlerFactories)
    {
        _autoMocker.Use(handlerFactories);
        return _autoMocker.CreateInstance<MessageDispatcher>();
    }

    [Fact]
    public async Task Dispatches_to_the_handler_for_the_message_type()
    {
        var sampleHandler = new SampleHandler();
        var otherHandler = new OtherHandler();
        var dispatcher = Subject(() => otherHandler, () => sampleHandler);

        await dispatcher.DispatchAsync(new SampleRequest());

        sampleHandler.WasCalled.Should().BeTrue();
        otherHandler.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_when_no_handler_exists_for_the_message_type()
    {
        var dispatcher = Subject(() => new OtherHandler());

        var dispatch = async () => await dispatcher.DispatchAsync(new SampleRequest());

        await dispatch.Should().ThrowAsync<UnknownMessageException>();
    }

    [Fact]
    public async Task Constructs_a_fresh_handler_instance_for_every_dispatch()
    {
        var constructed = new List<SampleHandler>();
        var dispatcher = Subject(() =>
        {
            var handler = new SampleHandler();
            constructed.Add(handler);
            return handler;
        });

        await dispatcher.DispatchAsync(new SampleRequest());
        await dispatcher.DispatchAsync(new SampleRequest());

        constructed.Count(h => h.WasCalled).Should().Be(2);
    }

    private record SampleRequest : IProtocolMessage;

    private record OtherRequest : IProtocolMessage;

    private class SampleHandler : MessageHandler<SampleRequest>
    {
        public bool WasCalled { get; private set; }

        public override Task ProcessAsync(SampleRequest message)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private class OtherHandler : MessageHandler<OtherRequest>
    {
        public bool WasCalled { get; private set; }

        public override Task ProcessAsync(OtherRequest message)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }
}
