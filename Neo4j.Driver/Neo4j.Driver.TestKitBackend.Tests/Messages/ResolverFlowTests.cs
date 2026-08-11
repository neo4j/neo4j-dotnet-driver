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
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResolverFlowTests
{
    [Fact]
    public void Resolve_requests_a_callback_with_the_asked_address_and_parses_the_reply()
    {
        ICorrelatedRequest? capturedRequest = null;
        var roundTripMock = new Mock<IReverseRoundTrip>();
        roundTripMock
            .Setup(r => r.SendExpectingAsync<string[]>(It.IsAny<ICorrelatedRequest>()))
            .Callback<ICorrelatedRequest>(request => capturedRequest = request)
            .ReturnsAsync(["hosta:9002", "hostb:9003"]);

        var resolver = new TestKitServerAddressResolver(roundTripMock.Object);

        var resolved = resolver.Resolve(ServerAddress.From("router1", 9001));

        var request = capturedRequest.Should().BeOfType<ResolverResolutionRequired>().Subject;
        request.Address.Should().Be("router1:9001");

        resolved.Should().BeEquivalentTo(
            new HashSet<ServerAddress> { ServerAddress.From("hosta", 9002), ServerAddress.From("hostb", 9003) });
    }

    [Fact]
    public void ResolverResolutionCompleted_fulfils_the_expectation_for_its_request_id()
    {
        var expectationsMock = new Mock<IExpectationStore>();
        var handler = new ResolverResolutionCompletedHandler(expectationsMock.Object);
        var message = new ResolverResolutionCompleted { RequestId = "callback-1", Addresses = ["hosta:9002"] };

        handler.ProcessAsync(message);

        expectationsMock.Verify(e => e.Fulfil("callback-1", message.Addresses), Times.Once);
    }
}
