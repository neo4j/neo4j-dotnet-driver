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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Expectations;

public class OutboundRoundTripTests
{
    private record FakePrompt : IProtocolMessage;

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<OutboundRoundTrip>();
    private readonly IExpectationStore _expectationStore;

    public OutboundRoundTripTests()
    {
        _expectationStore = _autoMocker.CreateInstance<ExpectationStore>();
        _autoMocker.Use(_expectationStore);
    }

    [Fact]
    public async Task SendExpectingAsync_writes_the_prompt_after_registering_the_expectation_then_completes_with_the_fulfilled_value()
    {
        var prompt = new FakePrompt();
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(prompt))
            .Returns(() =>
            {
                // If the expectation weren't already registered at this point, this would throw
                // TestKitProtocolException for an unknown key instead of succeeding.
                _expectationStore.Fulfil("key-1", "value-1");
                return Task.CompletedTask;
            });

        var roundTrip = _autoMocker.CreateInstance<OutboundRoundTrip>();

        var value = await roundTrip.SendExpectingAsync<string>(prompt, "key-1");

        value.Should().Be("value-1");
        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(prompt), Times.Once);
    }

    [Fact]
    public async Task The_correlated_overload_stamps_a_fresh_id_on_the_message_and_expects_on_it()
    {
        var prompt = new FakeCorrelatedPrompt();
        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(prompt))
            .Returns(Task.CompletedTask);

        var roundTrip = _autoMocker.CreateInstance<OutboundRoundTrip>();

        var task = roundTrip.SendExpectingAsync<string>(prompt);
        _expectationStore.Fulfil(prompt.Id, "value-1");
        var value = await task;

        value.Should().Be("value-1");
        prompt.Id.Should().NotBeNullOrEmpty();
    }

    private record FakeCorrelatedPrompt : ICorrelatedRequest
    {
        public string Id { get; set; } = "";
    }
}
