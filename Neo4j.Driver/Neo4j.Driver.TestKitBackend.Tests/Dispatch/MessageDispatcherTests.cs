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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Dispatch;

public class MessageDispatcherTests
{
    private readonly FakeHandlerIndex _handlers = new();

    [Fact]
    public async Task Dispatches_to_the_handler_for_the_message_type()
    {
        var sampleHandler = new SampleHandler();
        var otherHandler = new OtherHandler();
        _handlers.Add<SampleRequest>(() => sampleHandler);
        _handlers.Add<OtherRequest>(() => otherHandler);
        var dispatcher = new MessageDispatcher(_handlers);

        await dispatcher.DispatchAsync(new SampleRequest());

        sampleHandler.WasCalled.Should().BeTrue();
        otherHandler.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_when_no_handler_exists_for_the_message_type()
    {
        _handlers.Add<OtherRequest>(() => new OtherHandler());
        var dispatcher = new MessageDispatcher(_handlers);

        var dispatch = async () => await dispatcher.DispatchAsync(new SampleRequest());

        await dispatch.Should().ThrowAsync<UnknownMessageException>();
    }

    [Fact]
    public async Task Looks_a_handler_up_afresh_for_every_dispatch()
    {
        var constructed = new List<SampleHandler>();
        _handlers.Add<SampleRequest>(() =>
        {
            var handler = new SampleHandler();
            constructed.Add(handler);
            return handler;
        });

        var dispatcher = new MessageDispatcher(_handlers);

        await dispatcher.DispatchAsync(new SampleRequest());
        await dispatcher.DispatchAsync(new SampleRequest());

        constructed.Count(h => h.WasCalled).Should().Be(2);
    }

    private class FakeHandlerIndex : IIndex<Type, IMessageHandler>
    {
        private readonly Dictionary<Type, Func<IMessageHandler>> _factories = new();

        public IMessageHandler this[Type key] => _factories[key]();

        public void Add<TMessage>(Func<IMessageHandler> factory) where TMessage : IProtocolMessage
        {
            _factories[typeof(TMessage)] = factory;
        }

        public bool TryGetValue(Type key, out IMessageHandler value)
        {
            if (_factories.TryGetValue(key, out var factory))
            {
                value = factory();
                return true;
            }

            value = null!;
            return false;
        }
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
