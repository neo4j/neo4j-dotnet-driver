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
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Continuations;

public class BackgroundOperationHandlerTests
{
    private record TestRequest : IProtocolMessage;

    private record NestedResponse : IProtocolMessage;

    private record TerminalResponse : IProtocolMessage;

    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();

    private class BlockingHandler : BackgroundOperationHandler<TestRequest>
    {
        private readonly TaskCompletionSource _gate;

        public BlockingHandler(
            TaskCompletionSource gate,
            IContinuationCoordinator coordinator,
            IResponseWriter responseWriter,
            IDriverErrorMapper driverErrorMapper,
            ILogger logger)
            : base(coordinator, responseWriter, driverErrorMapper, logger)
        {
            _gate = gate;
        }

        protected override async Task<IProtocolMessage> ExecuteAsync(TestRequest message)
        {
            await _gate.Task;
            return new TerminalResponse();
        }
    }

    private class ThrowingHandler : BackgroundOperationHandler<TestRequest>
    {
        private readonly Exception _exception;

        public ThrowingHandler(
            Exception exception,
            IContinuationCoordinator coordinator,
            IResponseWriter responseWriter,
            IDriverErrorMapper driverErrorMapper,
            ILogger logger)
            : base(coordinator, responseWriter, driverErrorMapper, logger)
        {
            _exception = exception;
        }

        protected override Task<IProtocolMessage> ExecuteAsync(TestRequest message)
        {
            throw _exception;
        }
    }

    [Fact]
    public async Task Reports_DriverError_for_a_time_zone_not_found_exception_from_the_background_operation()
    {
        // Mirrors the crash caught by test_unknown_zoned_date_time: SessionReadTransactionHandler
        // (a BackgroundOperationHandler) touches a ZonedDateTime with an unrecognized IANA zone id
        // while building the response, which raises a raw TimeZoneNotFoundException.
        var exception = new TimeZoneNotFoundException("The time zone ID 'Europe/Neo4j' was not found");
        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "TimeZoneNotFoundException" };
        var driverErrorMapperMock = new Mock<IDriverErrorMapper>();
        driverErrorMapperMock.Setup(m => m.Map(exception)).Returns(errorResponse);

        var handler = new ThrowingHandler(
            exception,
            _coordinator,
            _responseWriterMock.Object,
            driverErrorMapperMock.Object,
            Mock.Of<ILogger>());

        await WithTimeoutAsync(handler.ProcessAsync(new TestRequest()));

        _responseWriterMock.Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }

    [Fact]
    public async Task A_mapper_failure_still_completes_the_slot_with_a_bare_BackendError()
    {
        // If _driverErrorMapper.Map itself throws, that exception used to escape the
        // fire-and-forget background task unobserved, leaving the response slot registered
        // forever and hanging the connection until testkit's own receive timeout.
        var exception = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        var driverErrorMapperMock = new Mock<IDriverErrorMapper>();
        driverErrorMapperMock.Setup(m => m.Map(exception)).Throws(new Exception("mapper bug"));

        var handler = new ThrowingHandler(
            exception,
            _coordinator,
            _responseWriterMock.Object,
            driverErrorMapperMock.Object,
            Mock.Of<ILogger>());

        await WithTimeoutAsync(handler.ProcessAsync(new TestRequest()));

        _responseWriterMock.Verify(
            w => w.WriteAsync(new BackendErrorResponse { Msg = exception.Message }),
            Times.Once);
    }

    [Fact]
    public async Task A_nested_response_can_complete_the_slot_before_the_background_operation_itself_finishes()
    {
        // Mirrors the retry flow: RetryableTransactionHandler's ExecuteAsync can still be
        // suspended (awaiting RetryablePositive/RetryableNegative) when a nested top-level
        // request (e.g. TransactionRun) needs to complete the response slot in its place.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new BlockingHandler(
            gate,
            _coordinator,
            _responseWriterMock.Object,
            Mock.Of<IDriverErrorMapper>(),
            Mock.Of<ILogger>());

        var processTask = handler.ProcessAsync(new TestRequest());

        _coordinator.CompleteNextResponse(new NestedResponse());
        await WithTimeoutAsync(processTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new NestedResponse()), Times.Once);

        var nextResponseTask = _coordinator.WaitForNextResponseAsync();
        gate.SetResult();

        (await WithTimeoutAsync(nextResponseTask)).Should().BeOfType<TerminalResponse>();
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
