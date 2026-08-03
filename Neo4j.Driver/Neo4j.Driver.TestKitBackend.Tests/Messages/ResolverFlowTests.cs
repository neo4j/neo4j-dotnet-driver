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
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ResolverFlowTests
{
    [Fact]
    public void Resolve_requests_a_callback_with_the_asked_address_and_parses_the_reply()
    {
        Func<string, ICallbackRequest>? capturedRequest = null;
        var callbacksMock = new Mock<ICallbackExchanger>();
        callbacksMock
            .Setup(c => c.SendAsync<ResolverResolutionCompletedRequest>(It.IsAny<Func<string, ICallbackRequest>>()))
            .Callback<Func<string, ICallbackRequest>>(f => capturedRequest = f)
            .ReturnsAsync(
                new ResolverResolutionCompletedRequest
                {
                    RequestId = "callback-1",
                    Addresses = ["hosta:9002", "hostb:9003"]
                });

        var resolver = new TestKitServerAddressResolver(callbacksMock.Object);

        var resolved = resolver.Resolve(ServerAddress.From("router1", 9001));

        Assert.NotNull(capturedRequest);
        var request = Assert.IsType<ResolverResolutionRequired>(capturedRequest!("callback-1"));
        Assert.Equal("router1:9001", request.Address);

        Assert.Equal(
            new HashSet<ServerAddress> { ServerAddress.From("hosta", 9002), ServerAddress.From("hostb", 9003) },
            resolved);
    }
}
