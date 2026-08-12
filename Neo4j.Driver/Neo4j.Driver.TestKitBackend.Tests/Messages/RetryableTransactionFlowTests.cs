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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class RetryableTransactionFlowTests
{
    private readonly ContinuationCoordinator _coordinator = new();
    private readonly Mock<IObjectStore> _objectStoreMock = new();
    private readonly Mock<ITransactionConfigMapper> _transactionConfigMapperMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();
    private readonly Mock<IConnectionInput> _connectionInputMock = new();
    private readonly Mock<IMessageSerializer> _serializerMock = new();
    private readonly Mock<IDriverErrorMapper> _driverErrorMapperMock = new();
    private readonly Mock<IExceptionOriginClassifier> _originClassifierMock = new();
    private readonly Mock<IAsyncSession> _sessionMock = new();
    private readonly Stored<IAsyncSession> _sessionHandle;

    public RetryableTransactionFlowTests()
    {
        _sessionHandle = new Stored<IAsyncSession>("session-1", _sessionMock.Object);
        _transactionConfigMapperMock
            .Setup(m => m.Map(It.IsAny<Dictionary<string, ICypherValue>?>(), It.IsAny<Optional<long?>>()))
            .Returns((Action<TransactionConfigBuilder>)(_ => { }));
    }

    [Fact]
    public async Task A_single_successful_attempt_round_trips_RetryableTry_then_RetryableDone()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task The_mapped_tx_meta_and_timeout_reach_ExecuteReadAsync()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["k"] = new CypherString("v") };
        var timeout = Optional<long?>.Specified(17);
        Action<TransactionConfigBuilder> configure = _ => { };
        _transactionConfigMapperMock.Setup(m => m.Map(txMeta, timeout)).Returns(configure);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), configure))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(
                new SessionReadTransactionRequest { Session = _sessionHandle, TxMeta = txMeta, Timeout = timeout }));

        _sessionMock.Verify(
            s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), configure),
            Times.Once);
    }

    [Fact]
    public async Task A_write_flow_where_the_driver_retries_round_trips_a_second_RetryableTry_before_RetryableDone()
    {
        var firstTxMock = StoreTx("tx-1");
        var secondTxMock = StoreTx("tx-2");

        _sessionMock
            .Setup(
                s => s.ExecuteWriteAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
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
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-2")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task The_mapped_tx_meta_and_timeout_reach_ExecuteWriteAsync()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["k"] = new CypherString("v") };
        var timeout = Optional<long?>.Specified(17);
        Action<TransactionConfigBuilder> configure = _ => { };
        _transactionConfigMapperMock.Setup(m => m.Map(txMeta, timeout)).Returns(configure);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), configure))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            WriteHandler().ProcessAsync(
                new SessionWriteTransactionRequest { Session = _sessionHandle, TxMeta = txMeta, Timeout = timeout }));

        _sessionMock.Verify(
            s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), configure),
            Times.Once);
    }

    [Fact]
    public async Task Negative_with_a_stored_error_the_driver_does_not_retry_terminates_with_DriverError()
    {
        var storedException = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(new Stored<Exception>("error-1", storedException));

        var errorResponse = new DriverErrorResponse { Id = "error-2", ErrorType = "ClientError" };
        _driverErrorMapperMock.Setup(m => m.Map(storedException)).Returns(errorResponse);
        _originClassifierMock.Setup(c => c.OriginatesInDriver(storedException)).Returns(true);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(
                new RetryableNegativeRequest(_sessionHandle, "error-1")));

        _responseWriterMock.Verify(w => w.WriteAsync(errorResponse), Times.Once);
    }

    [Fact]
    public async Task Negative_with_a_retryable_stored_error_lets_the_driver_retry_with_a_new_RetryableTry()
    {
        var storedException = new TransientException("Neo.TransientError.General.Whatever", "try again");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(new Stored<Exception>("error-1", storedException));

        var firstTxMock = StoreTx("tx-1");
        var secondTxMock = StoreTx("tx-2");

        _sessionMock
            .Setup(
                s => s.ExecuteWriteAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
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
                new RetryableNegativeRequest(_sessionHandle, "error-1")));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-2")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    private record FakeCallbackRequest(string Id) : ICallbackRequest;

    private record FakeCallbackCompleted : ICallbackResponse
    {
        public required string RequestId { get; init; }
    }

    [Fact]
    public async Task A_one_shot_callback_fired_during_a_retryable_attempt_does_not_corrupt_the_retry_slot()
    {
        // Regression test for the composition of ICallbackExchange's direct write/read with the
        // retry flow's coordinator slot: a callback exchanged both before the first attempt and
        // between attempts must not disturb RetryableTry/RetryableDone ordering. FakeCallbackRequest/
        // Completed stand in for an arbitrary still-on-CallbackExchange family; every real family has
        // since converted to the expectation model.
        var writtenMessages = new List<IProtocolMessage>();
        string? lastRequestId = null;
        _responseWriterMock
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(
                m =>
                {
                    writtenMessages.Add(m);
                    if (m is FakeCallbackRequest request)
                    {
                        lastRequestId = request.Id;
                    }
                })
            .Returns(Task.CompletedTask);

        _connectionInputMock.Setup(i => i.ReadRequestAsync()).ReturnsAsync("completion");
        _serializerMock
            .Setup(s => s.Deserialize("completion"))
            .Returns(() => new FakeCallbackCompleted { RequestId = lastRequestId! });

        var callbackExchanger = new CallbackExchanger(
            _responseWriterMock.Object,
            _connectionInputMock.Object,
            _serializerMock.Object);

        var firstTxMock = StoreTx("tx-1");
        var secondTxMock = StoreTx("tx-2");

        _sessionMock
            .Setup(
                s => s.ExecuteWriteAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                async (work, _) =>
                {
                    await callbackExchanger.SendAsync<FakeCallbackCompleted>(id => new FakeCallbackRequest(id));

                    await work(firstTxMock.Object);

                    await callbackExchanger.SendAsync<FakeCallbackCompleted>(id => new FakeCallbackRequest(id));

                    await work(secondTxMock.Object);
                });

        await WithTimeoutAsync(
            WriteHandler().ProcessAsync(new SessionWriteTransactionRequest { Session = _sessionHandle }));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-1")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableTryResponse("tx-2")), Times.Once);

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);

        writtenMessages.OfType<FakeCallbackRequest>().Count().Should().Be(2);
        writtenMessages.Select(m => m.GetType()).Should().Equal(
            typeof(FakeCallbackRequest),
            typeof(RetryableTryResponse),
            typeof(FakeCallbackRequest),
            typeof(RetryableTryResponse),
            typeof(RetryableDoneResponse));
    }

    [Fact]
    public async Task Negative_with_an_empty_errorId_terminates_with_FrontendError()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(
                new RetryableNegativeRequest(_sessionHandle, "")));

        _responseWriterMock.Verify(w => w.WriteAsync(It.IsAny<FrontendErrorResponse>()), Times.Once);
        _objectStoreMock.Verify(r => r.Get<Exception>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task An_unstored_errorId_does_not_leave_the_response_slot_registered()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        _objectStoreMock
            .Setup(r => r.Get<Exception>("bad-id"))
            .Throws(new TestKitProtocolException("No object is stored with id 'bad-id'."));

        var act = () => NegativeHandler().ProcessAsync(new RetryableNegativeRequest(_sessionHandle, "bad-id"));
        await act.Should().ThrowAsync<TestKitProtocolException>();

        // Action, not Func<Task<T>>: asserting on the latter awaits the deliberately
        // never-completed task instead of just checking the synchronous registration.
        Action act2 = () => { _coordinator.WaitForNextResponseAsync(); };
        act2.Should().NotThrow();
    }

    [Fact]
    public async Task A_duplicate_RetryablePositive_does_not_leave_the_response_slot_registered()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle)));

        var act = () => PositiveHandler().ProcessAsync(new RetryablePositiveRequest(_sessionHandle));
        await act.Should().ThrowAsync<InvalidOperationException>();

        Action act2 = () => { _coordinator.WaitForNextResponseAsync(); };
        act2.Should().NotThrow();
    }

    [Fact]
    public async Task A_duplicate_RetryableNegative_does_not_leave_the_response_slot_registered()
    {
        var storedException = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(new Stored<Exception>("error-1", storedException));

        var errorResponse = new DriverErrorResponse { Id = "error-2", ErrorType = "ClientError" };
        _driverErrorMapperMock.Setup(m => m.Map(storedException)).Returns(errorResponse);
        _originClassifierMock.Setup(c => c.OriginatesInDriver(storedException)).Returns(true);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        await WithTimeoutAsync(
            ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionHandle }));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(new RetryableNegativeRequest(_sessionHandle, "error-1")));

        var act = () => NegativeHandler().ProcessAsync(new RetryableNegativeRequest(_sessionHandle, "error-1"));
        await act.Should().ThrowAsync<InvalidOperationException>();

        Action act2 = () => { _coordinator.WaitForNextResponseAsync(); };
        act2.Should().NotThrow();
    }

    private Mock<IAsyncTransaction> StoreTx(string id)
    {
        var txMock = new Mock<IAsyncTransaction>();
        _objectStoreMock
            .Setup(r => r.Store(txMock.Object))
            .Returns(new Stored<IAsyncTransaction>(id, txMock.Object));

        return txMock;
    }

    private SessionReadTransactionHandler ReadHandler()
    {
        return new SessionReadTransactionHandler(
            _objectStoreMock.Object,
            _coordinator,
            _transactionConfigMapperMock.Object,
            _responseWriterMock.Object,
            _driverErrorMapperMock.Object,
            _originClassifierMock.Object,
            Mock.Of<ILogger>());
    }

    private SessionWriteTransactionHandler WriteHandler()
    {
        return new SessionWriteTransactionHandler(
            _objectStoreMock.Object,
            _coordinator,
            _transactionConfigMapperMock.Object,
            _responseWriterMock.Object,
            _driverErrorMapperMock.Object,
            _originClassifierMock.Object,
            Mock.Of<ILogger>());
    }

    private RetryablePositiveHandler PositiveHandler()
    {
        return new RetryablePositiveHandler(_coordinator, _responseWriterMock.Object);
    }

    private RetryableNegativeHandler NegativeHandler()
    {
        return new RetryableNegativeHandler(_objectStoreMock.Object, _coordinator, _responseWriterMock.Object);
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
