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

using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Continuations;

public class ContinuationCoordinatorTests
{
    private record FakeResponse(string Tag) : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();

    [Fact]
    public async Task WaitForNextResponseAsync_completes_with_whatever_CompleteNextResponse_is_given()
    {
        var responseTask = _coordinator.WaitForNextResponseAsync();
        Assert.False(responseTask.IsCompleted);

        _coordinator.CompleteNextResponse(new FakeResponse("try-1"));

        var response = await WithTimeoutAsync(responseTask);
        Assert.Equal(new FakeResponse("try-1"), response);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_completes_when_CompleteOutcome_is_called_for_the_same_session()
    {
        var outcomeTask = _coordinator.WaitForOutcomeAsync("session-1");
        Assert.False(outcomeTask.IsCompleted);

        _coordinator.CompleteOutcome("session-1");

        await WithTimeoutAsync(outcomeTask);
    }

    [Fact]
    public async Task Outcomes_of_different_sessions_do_not_interfere_with_each_other()
    {
        var outcomeTaskA = _coordinator.WaitForOutcomeAsync("session-a");
        var outcomeTaskB = _coordinator.WaitForOutcomeAsync("session-b");

        _coordinator.CompleteOutcome("session-b");
        Assert.False(outcomeTaskA.IsCompleted);
        await WithTimeoutAsync(outcomeTaskB);

        _coordinator.CompleteOutcome("session-a");
        await WithTimeoutAsync(outcomeTaskA);
    }

    [Fact]
    public async Task The_response_handshake_can_run_more_than_once()
    {
        var firstResponseTask = _coordinator.WaitForNextResponseAsync();
        _coordinator.CompleteNextResponse(new FakeResponse("try-1"));
        await WithTimeoutAsync(firstResponseTask);

        var secondResponseTask = _coordinator.WaitForNextResponseAsync();
        Assert.False(secondResponseTask.IsCompleted);

        _coordinator.CompleteNextResponse(new FakeResponse("try-2"));
        var secondResponse = await WithTimeoutAsync(secondResponseTask);
        Assert.Equal(new FakeResponse("try-2"), secondResponse);
    }

    [Fact]
    public async Task FailOutcome_makes_the_pending_outcome_throw_the_given_exception()
    {
        var exception = new TransientException("code", "message");

        var outcomeTask = _coordinator.WaitForOutcomeAsync("session-1");

        _coordinator.FailOutcome("session-1", exception);

        var thrown = await Assert.ThrowsAsync<TransientException>(() => WithTimeoutAsync(outcomeTask));
        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task Registering_a_second_continuation_while_one_is_pending_fails_loudly()
    {
        _ = _coordinator.WaitForNextResponseAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.WaitForNextResponseAsync());

        _ = _coordinator.WaitForOutcomeAsync("session-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.WaitForOutcomeAsync("session-1"));
    }

    [Fact]
    public async Task Registered_callbacks_get_unique_ids_and_complete_independently()
    {
        var first = _coordinator.RegisterCallback();
        var second = _coordinator.RegisterCallback();

        Assert.NotEqual(first.Id, second.Id);

        _coordinator.CompleteCallback(second.Id, new FakeResponse("second"));
        Assert.False(first.Completion.IsCompleted);
        Assert.Equal(new FakeResponse("second"), await WithTimeoutAsync(second.Completion));

        _coordinator.CompleteCallback(first.Id, new FakeResponse("first"));
        Assert.Equal(new FakeResponse("first"), await WithTimeoutAsync(first.Completion));
    }

    [Fact]
    public void Completing_an_unknown_callback_fails_loudly()
    {
        Assert.Throws<InvalidOperationException>(
            () => _coordinator.CompleteCallback("no-such-id", new FakeResponse("orphan")));
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
