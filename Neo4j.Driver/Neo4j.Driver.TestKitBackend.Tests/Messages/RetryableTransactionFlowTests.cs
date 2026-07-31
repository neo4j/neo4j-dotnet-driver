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
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Retry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

// The retryable-flow handlers only make sense as a set — these tests pin the handshakes between
// them via a real IRetryCoordinator (the collaboration is the point; mocking the coordinator
// away would just re-assert the implementation).
public class RetryableTransactionFlowTests
{
    private readonly RetryCoordinator _coordinator = new();
    private readonly Mock<IRegistry> _registryMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();
    private readonly Mock<IDriverErrorMapper> _driverErrorMapperMock = new();
    private readonly Mock<IAsyncSession> _sessionMock = new();
    private readonly RegistryObject<IAsyncSession> _sessionHandle;

    public RetryableTransactionFlowTests()
    {
        _sessionHandle = new RegistryObject<IAsyncSession>("session-1", _sessionMock.Object);
    }

    [Fact]
    public async Task A_single_successful_attempt_round_trips_RetryableTry_then_RetryableDone()
    {
        var txMock = RegisterTx("tx-1");
        _sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task A_write_flow_where_the_driver_retries_round_trips_a_second_RetryableTry_before_RetryableDone()
    {
        var firstTxMock = RegisterTx("tx-1");
        var secondTxMock = RegisterTx("tx-2");

        // The driver's retry logic re-invokes the work function after a failed commit; two
        // sequential invocations are that behaviour distilled — each attempt gets its own tx.
        _sessionMock
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                async (work, _) =>
                {
                    await work(firstTxMock.Object);
                    await work(secondTxMock.Object);
                });

        await WithTimeoutAsync(
            WriteHandler().ProcessAsync(new SessionWriteTransactionRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-2")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task Negative_with_a_stored_error_the_driver_does_not_retry_terminates_with_DriverError()
    {
        var storedException = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        _registryMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(new RegistryObject<Exception>("error-1", storedException));

        var errorResponse = new DriverErrorResponse { Id = "error-2", ErrorType = "ClientError" };
        _driverErrorMapperMock.Setup(m => m.Map(storedException)).Returns(errorResponse);

        var txMock = RegisterTx("tx-1");
        _sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(
                new RetryableNegativeRequest { Session = _sessionHandle, ErrorId = "error-1" }));

        _responseWriterMock.Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }

    [Fact]
    public async Task Negative_with_a_retryable_stored_error_lets_the_driver_retry_with_a_new_RetryableTry()
    {
        var storedException = new TransientException("Neo.TransientError.General.Whatever", "try again");
        _registryMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(new RegistryObject<Exception>("error-1", storedException));

        var firstTxMock = RegisterTx("tx-1");
        var secondTxMock = RegisterTx("tx-2");

        // The driver retries when the work function throws a retryable error — the rethrown
        // stored exception must reach the driver's retry logic, not get eaten by the backend.
        _sessionMock
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                async (work, _) =>
                {
                    try
                    {
                        await work(firstTxMock.Object);
                    }
                    catch (TransientException)
                    {
                        await work(secondTxMock.Object);
                    }
                });

        await WithTimeoutAsync(
            WriteHandler().ProcessAsync(new SessionWriteTransactionRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(
                new RetryableNegativeRequest { Session = _sessionHandle, ErrorId = "error-1" }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-2")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task Negative_with_an_empty_errorId_terminates_with_FrontendError()
    {
        var txMock = RegisterTx("tx-1");
        _sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), null))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(
                new RetryableNegativeRequest { Session = _sessionHandle, ErrorId = "" }));

        _responseWriterMock.Verify(w => w.WriteAsync(It.IsAny<FrontendErrorResponse>()), Times.Once);
        _registryMock.Verify(r => r.Get<Exception>(It.IsAny<string>()), Times.Never);
    }

    private Mock<IAsyncTransaction> RegisterTx(string id)
    {
        var txMock = new Mock<IAsyncTransaction>();
        _registryMock
            .Setup(r => r.Register(txMock.Object))
            .Returns(new RegistryObject<IAsyncTransaction>(id, txMock.Object));

        return txMock;
    }

    private SessionReadTransactionHandler ReadHandler()
    {
        return new SessionReadTransactionHandler(
            _registryMock.Object,
            _coordinator,
            _responseWriterMock.Object,
            _driverErrorMapperMock.Object,
            Mock.Of<ILogger>());
    }

    private SessionWriteTransactionHandler WriteHandler()
    {
        return new SessionWriteTransactionHandler(
            _registryMock.Object,
            _coordinator,
            _responseWriterMock.Object,
            _driverErrorMapperMock.Object,
            Mock.Of<ILogger>());
    }

    private RetryablePositiveHandler PositiveHandler()
    {
        return new RetryablePositiveHandler(_coordinator, _responseWriterMock.Object);
    }

    private RetryableNegativeHandler NegativeHandler()
    {
        return new RetryableNegativeHandler(_registryMock.Object, _coordinator, _responseWriterMock.Object);
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
