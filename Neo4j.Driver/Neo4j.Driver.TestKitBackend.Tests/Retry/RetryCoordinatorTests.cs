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

using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Retry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Retry;

public class RetryCoordinatorTests
{
    private record FakeResponse(string Tag) : IProtocolMessage;

    [Fact]
    public async Task WaitForNextResponseAsync_completes_with_whatever_CompleteNextResponse_is_given()
    {
        var coordinator = new RetryCoordinator();

        var responseTask = coordinator.WaitForNextResponseAsync("session-1");
        Assert.False(responseTask.IsCompleted);

        coordinator.CompleteNextResponse("session-1", new FakeResponse("try-1"));

        var response = await WithTimeoutAsync(responseTask);
        Assert.Equal(new FakeResponse("try-1"), response);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_completes_when_CompleteOutcome_is_called_for_the_same_session()
    {
        var coordinator = new RetryCoordinator();

        var outcomeTask = coordinator.WaitForOutcomeAsync("session-1");
        Assert.False(outcomeTask.IsCompleted);

        coordinator.CompleteOutcome("session-1");

        await WithTimeoutAsync(outcomeTask);
    }

    [Fact]
    public async Task Different_sessions_do_not_interfere_with_each_other()
    {
        var coordinator = new RetryCoordinator();

        var responseTaskA = coordinator.WaitForNextResponseAsync("session-a");
        var responseTaskB = coordinator.WaitForNextResponseAsync("session-b");

        coordinator.CompleteNextResponse("session-b", new FakeResponse("b"));
        Assert.False(responseTaskA.IsCompleted);

        var responseB = await WithTimeoutAsync(responseTaskB);
        Assert.Equal(new FakeResponse("b"), responseB);

        coordinator.CompleteNextResponse("session-a", new FakeResponse("a"));
        var responseA = await WithTimeoutAsync(responseTaskA);
        Assert.Equal(new FakeResponse("a"), responseA);
    }

    [Fact]
    public async Task A_session_can_run_through_the_response_handshake_more_than_once()
    {
        var coordinator = new RetryCoordinator();

        var firstResponseTask = coordinator.WaitForNextResponseAsync("session-1");
        coordinator.CompleteNextResponse("session-1", new FakeResponse("try-1"));
        await WithTimeoutAsync(firstResponseTask);

        var secondResponseTask = coordinator.WaitForNextResponseAsync("session-1");
        Assert.False(secondResponseTask.IsCompleted);

        coordinator.CompleteNextResponse("session-1", new FakeResponse("try-2"));
        var secondResponse = await WithTimeoutAsync(secondResponseTask);
        Assert.Equal(new FakeResponse("try-2"), secondResponse);
    }

    [Fact]
    public async Task Registering_a_second_continuation_for_a_session_that_already_has_one_pending_fails_loudly()
    {
        var coordinator = new RetryCoordinator();

        _ = coordinator.WaitForNextResponseAsync("session-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.WaitForNextResponseAsync("session-1"));

        _ = coordinator.WaitForOutcomeAsync("session-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.WaitForOutcomeAsync("session-1"));
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
