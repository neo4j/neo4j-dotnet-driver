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

using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Continuations;

public class DetachedOperationHandlerTests
{
    private record TestRequest : IProtocolMessage;

    private record CallbackMessage(string Id) : IProtocolMessage;

    private record CallbackCompletion : IProtocolMessage;

    private record TerminalMessage : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    // Plays a driver operation whose seam is synchronous end-to-end (e.g. IServerAddressResolver
    // during routing discovery): it blocks its calling thread on a callback completion before
    // ever yielding, so it runs entirely in the detached task's synchronous prefix.
    private class SynchronouslyBlockingHandler : DetachedOperationHandler<TestRequest>
    {
        private readonly IContinuationCoordinator _coordinator;

        public SynchronouslyBlockingHandler(
            IContinuationCoordinator coordinator,
            IResponseWriter responseWriter,
            IDriverErrorMapper driverErrorMapper,
            ILogger logger)
            : base(coordinator, responseWriter, driverErrorMapper, logger)
        {
            _coordinator = coordinator;
        }

        protected override Task<IProtocolMessage> ExecuteAsync(TestRequest message)
        {
            var pending = _coordinator.RegisterCallback();
            _coordinator.CompleteNextResponse(new CallbackMessage(pending.Id));
            pending.Completion.GetAwaiter().GetResult();
            return Task.FromResult<IProtocolMessage>(new TerminalMessage());
        }
    }

    [Fact]
    public async Task An_operation_blocking_synchronously_on_a_callback_does_not_deadlock_the_loop()
    {
        CallbackMessage? written = null;
        _responseWriterMock
            .Setup(w => w.WriteAsync(It.IsAny<CallbackMessage>()))
            .Callback<IProtocolMessage>(m => written = (CallbackMessage)m)
            .Returns(Task.CompletedTask);

        var handler = new SynchronouslyBlockingHandler(
            _coordinator,
            _responseWriterMock.Object,
            Mock.Of<IDriverErrorMapper>(),
            Mock.Of<ILogger>());

        // The message loop calls ProcessAsync inline; it must come back with the callback
        // request written even though the operation is still blocked waiting for the completion.
        var processTask = Task.Run(
            () => handler.ProcessAsync(new TestRequest()),
            TestContext.Current.CancellationToken);
        await WithTimeoutAsync(processTask);

        Assert.NotNull(written);

        // Play the completed handler: hold the response slot, then resolve the callback — the
        // unblocked operation finishes and its terminal response lands in the held slot.
        var nextResponseTask = _coordinator.WaitForNextResponseAsync();
        _coordinator.CompleteCallback(written!.Id, new CallbackCompletion());

        Assert.IsType<TerminalMessage>(await WithTimeoutAsync(nextResponseTask));
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
