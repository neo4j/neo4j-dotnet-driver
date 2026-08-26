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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Serialization;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class RetryableTransactionFlowTests
{
    private readonly ExpectationStore _expectationStore = new(Mock.Of<ILogger>());
    private readonly OutboundRoundTrip _roundTrip;
    private readonly Mock<IObjectStore> _objectStoreMock = new();
    private readonly Mock<ITransactionConfigMapper> _transactionConfigMapperMock = new();
    private readonly Mock<IResponseWriter> _responseWriterMock = new();
    private readonly Mock<IAsyncSession> _sessionMock = new();

    private readonly Lock _writtenLock = new();
    private readonly List<IProtocolMessage> _written = [];
    private readonly List<(IProtocolMessage Expected, TaskCompletionSource Signal)> _writeWaiters = [];

    public RetryableTransactionFlowTests()
    {
        _roundTrip = new OutboundRoundTrip(_expectationStore, _responseWriterMock.Object);
        _transactionConfigMapperMock
            .Setup(m => m.Map(It.IsAny<Dictionary<string, ICypherValue>?>(), It.IsAny<Optional<long?>>()))
            .Returns((Action<TransactionConfigBuilder>)(_ => { }));

        _responseWriterMock
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .Callback<IProtocolMessage>(RecordWrite)
            .Returns(Task.CompletedTask);
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

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

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

        var handlerTask = ReadHandler().ProcessAsync(
            new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1", TxMeta = txMeta, Timeout = timeout });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

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

        var handlerTask = WriteHandler().ProcessAsync(new SessionWriteTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WaitForWriteAsync(new RetryableTryResponse("tx-2"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

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

        var handlerTask = WriteHandler().ProcessAsync(
            new SessionWriteTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1", TxMeta = txMeta, Timeout = timeout });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

        _sessionMock.Verify(
            s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncQueryRunner, Task>>(), configure),
            Times.Once);
    }

    [Fact]
    public async Task Negative_with_a_stored_error_the_driver_does_not_retry_faults_the_handler_with_that_error()
    {
        // The loop's tracked-task path owns mapping the fault to a DriverError response
        // (MessageLoopTests); the flow's job is to surface the stored exception unchanged.
        var storedException = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(storedException);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "error-1")));

        var act = () => WithTimeoutAsync(handlerTask);
        (await act.Should().ThrowAsync<ClientException>()).Which.Should().BeSameAs(storedException);
    }

    [Fact]
    public async Task Negative_with_a_retryable_stored_error_lets_the_driver_retry_with_a_new_RetryableTry()
    {
        var storedException = new TransientException("Neo.TransientError.General.Whatever", "try again");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(storedException);

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

        var handlerTask = WriteHandler().ProcessAsync(new SessionWriteTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "error-1")));

        await WaitForWriteAsync(new RetryableTryResponse("tx-2"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task Negative_with_an_empty_errorId_faults_the_handler_with_FrontendException()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "")));

        var act = () => WithTimeoutAsync(handlerTask);
        await act.Should().ThrowAsync<FrontendException>();

        _objectStoreMock.Verify(r => r.Get<Exception>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task An_unstored_errorId_leaves_the_parked_attempt_fulfillable()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        _objectStoreMock
            .Setup(r => r.Get<Exception>("bad-id"))
            .Throws(new TestKitProtocolException("No object is stored with id 'bad-id'."));

        var act = () => NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "bad-id"));
        await act.Should().ThrowAsync<TestKitProtocolException>();

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

        _responseWriterMock.Verify(w => w.WriteAsync(new RetryableDoneResponse()), Times.Once);
    }

    [Fact]
    public async Task A_duplicate_RetryablePositive_fails_loudly_on_the_unknown_key()
    {
        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1")));

        await WithTimeoutAsync(handlerTask);

        var act = () => PositiveHandler().ProcessAsync(new RetryablePositiveRequest("session-1"));
        (await act.Should().ThrowAsync<TestKitProtocolException>()).WithMessage("*session-1*");
    }

    [Fact]
    public async Task A_duplicate_RetryableNegative_fails_loudly_on_the_unknown_key()
    {
        var storedException = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        _objectStoreMock
            .Setup(r => r.Get<Exception>("error-1"))
            .Returns(storedException);

        var txMock = StoreTx("tx-1");
        _sessionMock
            .Setup(
                s => s.ExecuteReadAsync(
                    It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                    It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncQueryRunner, Task>, Action<TransactionConfigBuilder>>(
                (work, _) => work(txMock.Object));

        var handlerTask = ReadHandler().ProcessAsync(new SessionReadTransactionRequest { Session = _sessionMock.Object, SessionId = "session-1" });

        await WaitForWriteAsync(new RetryableTryResponse("tx-1"));

        await WithTimeoutAsync(
            NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "error-1")));

        var faulted = () => WithTimeoutAsync(handlerTask);
        await faulted.Should().ThrowAsync<ClientException>();

        var act = () => NegativeHandler().ProcessAsync(new RetryableNegativeRequest("session-1", "error-1"));
        (await act.Should().ThrowAsync<TestKitProtocolException>()).WithMessage("*session-1*");
    }

    private Mock<IAsyncTransaction> StoreTx(string id)
    {
        var txMock = new Mock<IAsyncTransaction>();
        _objectStoreMock
            .Setup(r => r.Store(txMock.Object))
            .Returns(id);

        return txMock;
    }

    private SessionReadTransactionHandler ReadHandler()
    {
        return new SessionReadTransactionHandler(
            _objectStoreMock.Object,
            _roundTrip,
            _transactionConfigMapperMock.Object,
            _responseWriterMock.Object);
    }

    private SessionWriteTransactionHandler WriteHandler()
    {
        return new SessionWriteTransactionHandler(
            _objectStoreMock.Object,
            _roundTrip,
            _transactionConfigMapperMock.Object,
            _responseWriterMock.Object);
    }

    private RetryablePositiveHandler PositiveHandler()
    {
        return new RetryablePositiveHandler(_expectationStore);
    }

    private RetryableNegativeHandler NegativeHandler()
    {
        return new RetryableNegativeHandler(_objectStoreMock.Object, _expectationStore);
    }

    private void RecordWrite(IProtocolMessage message)
    {
        lock (_writtenLock)
        {
            _written.Add(message);
            foreach (var (expected, signal) in _writeWaiters.Where(waiter => waiter.Expected.Equals(message)))
            {
                signal.TrySetResult();
            }
        }
    }

    private Task WaitForWriteAsync(IProtocolMessage expected)
    {
        TaskCompletionSource signal;
        lock (_writtenLock)
        {
            if (_written.Contains(expected))
            {
                return Task.CompletedTask;
            }

            signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _writeWaiters.Add((expected, signal));
        }

        return WithTimeoutAsync(signal.Task);
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
