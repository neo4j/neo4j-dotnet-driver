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

using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface IRetryableTransactionRequest
{
    Stored<IAsyncSession> Session { get; }

    Dictionary<string, ICypherValue>? TxMeta { get; }

    Optional<long?> Timeout { get; }
}

internal record SessionReadTransactionRequest : IProtocolMessage, IRetryableTransactionRequest
{
    public required Stored<IAsyncSession> Session { get; init; }

    public Dictionary<string, ICypherValue>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record SessionWriteTransactionRequest : IProtocolMessage, IRetryableTransactionRequest
{
    public required Stored<IAsyncSession> Session { get; init; }

    public Dictionary<string, ICypherValue>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record RetryableTryResponse(string Id) : IProtocolMessage;

internal record RetryableDoneResponse : IProtocolMessage;

internal record RetryablePositiveRequest(Stored<IAsyncSession> Session) : IProtocolMessage;

internal record RetryableNegativeRequest(Stored<IAsyncSession> Session, string ErrorId) : IProtocolMessage;

internal enum RetryableOutcome
{
    Positive
}

internal abstract class RetryableTransactionHandler<T> : MessageHandler<T>
    where T : IProtocolMessage, IRetryableTransactionRequest
{
    private readonly IObjectStore _objectStore;
    private readonly IOutboundRoundTrip _roundTrip;
    private readonly ITransactionConfigMapper _transactionConfigMapper;
    private readonly IResponseWriter _responseWriter;

    protected RetryableTransactionHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        ITransactionConfigMapper transactionConfigMapper,
        IResponseWriter responseWriter)
    {
        _objectStore = objectStore;
        _roundTrip = roundTrip;
        _transactionConfigMapper = transactionConfigMapper;
        _responseWriter = responseWriter;
    }

    protected abstract Task ExecuteTransactionAsync(
        IAsyncSession session,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> configure);

    public override async Task ProcessAsync(T message)
    {
        var sessionId = message.Session.Id;

        await ExecuteTransactionAsync(
            message.Session.Object,
            runner => RunAttemptAsync(runner, sessionId),
            _transactionConfigMapper.Map(message.TxMeta, message.Timeout));

        await _responseWriter.WriteAsync(new RetryableDoneResponse());
    }

    private async Task RunAttemptAsync(IAsyncQueryRunner runner, string sessionId)
    {
        var stored = _objectStore.Store((IAsyncTransaction)runner);
        await _roundTrip.SendExpectingAsync<RetryableOutcome>(new RetryableTryResponse(stored.Id), sessionId);
    }
}

internal class SessionReadTransactionHandler : RetryableTransactionHandler<SessionReadTransactionRequest>
{
    public SessionReadTransactionHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        ITransactionConfigMapper transactionConfigMapper,
        IResponseWriter responseWriter)
        : base(objectStore, roundTrip, transactionConfigMapper, responseWriter)
    {
    }

    protected override Task ExecuteTransactionAsync(
        IAsyncSession session,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> configure)
    {
        return session.ExecuteReadAsync(work, configure);
    }
}

internal class SessionWriteTransactionHandler : RetryableTransactionHandler<SessionWriteTransactionRequest>
{
    public SessionWriteTransactionHandler(
        IObjectStore objectStore,
        IOutboundRoundTrip roundTrip,
        ITransactionConfigMapper transactionConfigMapper,
        IResponseWriter responseWriter)
        : base(objectStore, roundTrip, transactionConfigMapper, responseWriter)
    {
    }

    protected override Task ExecuteTransactionAsync(
        IAsyncSession session,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> configure)
    {
        return session.ExecuteWriteAsync(work, configure);
    }
}

internal class RetryablePositiveHandler : MessageHandler<RetryablePositiveRequest>
{
    private readonly IExpectationStore _expectationStore;

    public RetryablePositiveHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(RetryablePositiveRequest message)
    {
        _expectationStore.Fulfil(message.Session.Id, RetryableOutcome.Positive);
        return Task.CompletedTask;
    }
}

internal class RetryableNegativeHandler : MessageHandler<RetryableNegativeRequest>
{
    private readonly IObjectStore _objectStore;
    private readonly IExpectationStore _expectationStore;

    public RetryableNegativeHandler(IObjectStore objectStore, IExpectationStore expectationStore)
    {
        _objectStore = objectStore;
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(RetryableNegativeRequest message)
    {
        var exception = message.ErrorId == ""
            ? new FrontendException("Error from client in retryable tx")
            : _objectStore.Get<Exception>(message.ErrorId).Object;

        _expectationStore.Fail(message.Session.Id, exception);
        return Task.CompletedTask;
    }
}
