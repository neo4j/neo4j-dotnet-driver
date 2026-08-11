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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Expectations;

public class ReverseRoundTripTests
{
    private record FakePrompt : IProtocolMessage;

    [Fact]
    public async Task SendExpectingAsync_writes_the_prompt_after_registering_the_expectation_then_completes_with_the_fulfilled_value()
    {
        var expectations = new ExpectationStore(NullLogger.Instance);
        var writer = new Mock<IResponseWriter>();
        var prompt = new FakePrompt();
        writer
            .Setup(w => w.WriteAsync(prompt))
            .Returns(() =>
            {
                // If the expectation weren't already registered at this point, this would throw
                // TestKitProtocolException for an unknown key instead of succeeding.
                expectations.Fulfil("key-1", "value-1");
                return Task.CompletedTask;
            });

        var roundTrip = new ReverseRoundTrip(expectations, writer.Object);

        var value = await roundTrip.SendExpectingAsync<string>(prompt, "key-1");

        value.Should().Be("value-1");
        writer.Verify(w => w.WriteAsync(prompt), Times.Once);
    }
}
