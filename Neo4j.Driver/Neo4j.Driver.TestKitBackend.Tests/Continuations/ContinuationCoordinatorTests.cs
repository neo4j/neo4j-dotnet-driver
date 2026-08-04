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
        responseTask.IsCompleted.Should().BeFalse();

        _coordinator.CompleteNextResponse(new FakeResponse("try-1"));

        var response = await WithTimeoutAsync(responseTask);
        response.Should().Be(new FakeResponse("try-1"));
    }

    [Fact]
    public async Task WaitForOutcomeAsync_completes_when_CompleteOutcome_is_called_for_the_same_session()
    {
        var outcomeTask = _coordinator.WaitForOutcomeAsync("session-1");
        outcomeTask.IsCompleted.Should().BeFalse();

        _coordinator.CompleteOutcome("session-1");

        await WithTimeoutAsync(outcomeTask);
    }

    [Fact]
    public async Task Outcomes_of_different_sessions_do_not_interfere_with_each_other()
    {
        var outcomeTaskA = _coordinator.WaitForOutcomeAsync("session-a");
        var outcomeTaskB = _coordinator.WaitForOutcomeAsync("session-b");

        _coordinator.CompleteOutcome("session-b");
        outcomeTaskA.IsCompleted.Should().BeFalse();
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
        secondResponseTask.IsCompleted.Should().BeFalse();

        _coordinator.CompleteNextResponse(new FakeResponse("try-2"));
        var secondResponse = await WithTimeoutAsync(secondResponseTask);
        secondResponse.Should().Be(new FakeResponse("try-2"));
    }

    [Fact]
    public async Task FailOutcome_makes_the_pending_outcome_throw_the_given_exception()
    {
        var exception = new TransientException("code", "message");

        var outcomeTask = _coordinator.WaitForOutcomeAsync("session-1");

        _coordinator.FailOutcome("session-1", exception);

        Func<Task> act = () => WithTimeoutAsync(outcomeTask);
        var thrown = await act.Should().ThrowAsync<TransientException>();
        thrown.Which.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task Registering_a_second_continuation_while_one_is_pending_fails_loudly()
    {
        _ = _coordinator.WaitForNextResponseAsync();
        Func<Task> waitForResponseAgain = () => _coordinator.WaitForNextResponseAsync();
        await waitForResponseAgain.Should().ThrowAsync<InvalidOperationException>();

        _ = _coordinator.WaitForOutcomeAsync("session-1");
        Func<Task> waitForOutcomeAgain = () => _coordinator.WaitForOutcomeAsync("session-1");
        await waitForOutcomeAgain.Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(task);
        return await task;
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(task);
        await task;
    }
}
