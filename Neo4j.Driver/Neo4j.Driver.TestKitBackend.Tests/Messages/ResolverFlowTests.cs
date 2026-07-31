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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

// The resolver and the completed handler only make sense as a pair — this pins the callback
// handshake between them via a real IContinuationCoordinator, playing the roles of the driver
// (resolving an address on a connection thread) and of the detached operation whose response
// slot the callback borrows.
public class ResolverFlowTests
{
    private record TerminalResponse(string Tag) : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    [Fact]
    public async Task Resolve_round_trips_a_callback_with_the_asked_address_and_parses_the_reply()
    {
        var resolver = new TestKitServerAddressResolver(_coordinator);

        var openRequestTask = _coordinator.WaitForNextResponseAsync();

        // The driver's resolver seam is synchronous, so Resolve blocks its calling thread —
        // play the driver by resolving on a worker thread.
        var resolveTask = Task.Run(
            () => resolver.Resolve(ServerAddress.From("router1", 9001)),
            TestContext.Current.CancellationToken);

        var callbackRequest = Assert.IsType<ResolverResolutionRequired>(await WithTimeoutAsync(openRequestTask));
        Assert.Equal("router1:9001", callbackRequest.Address);

        var completedHandler = new CallbackCompletedHandler<ResolverResolutionCompletedRequest>(
            _coordinator,
            _responseWriterMock.Object);

        var completedTask = completedHandler.ProcessAsync(
            new ResolverResolutionCompletedRequest
            {
                RequestId = callbackRequest.Id,
                Addresses = ["hosta:9002", "hostb:9003"]
            });

        var resolved = await WithTimeoutAsync(resolveTask);
        Assert.Equal(
            new HashSet<ServerAddress> { ServerAddress.From("hosta", 9002), ServerAddress.From("hostb", 9003) },
            resolved);

        // The resumed operation eventually produces the terminal response; the completed handler
        // is the one holding the response slot, so it writes it.
        _coordinator.CompleteNextResponse(new TerminalResponse("result"));
        await WithTimeoutAsync(completedTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new TerminalResponse("result")), Times.Once);
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        return await task;
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Same(task, completed);
        await task;
    }
}
