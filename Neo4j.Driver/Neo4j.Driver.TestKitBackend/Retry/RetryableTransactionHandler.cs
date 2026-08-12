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
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectStorage;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Retry;

internal interface IRetryableTransactionRequest
{
    Stored<IAsyncSession> Session { get; }

    Dictionary<string, ICypherValue>? TxMeta { get; }

    Optional<long?> Timeout { get; }
}

internal abstract class RetryableTransactionHandler<T> : BackgroundOperationHandler<T>
    where T : IProtocolMessage, IRetryableTransactionRequest
{
    private readonly IObjectStore _objectStore;
    private readonly IContinuationCoordinator _coordinator;
    private readonly ITransactionConfigMapper _transactionConfigMapper;

    protected RetryableTransactionHandler(
        IObjectStore objectStore,
        IContinuationCoordinator coordinator,
        ITransactionConfigMapper transactionConfigMapper,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        IExceptionOriginClassifier originClassifier,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, originClassifier, logger)
    {
        _objectStore = objectStore;
        _coordinator = coordinator;
        _transactionConfigMapper = transactionConfigMapper;
    }

    protected abstract Task ExecuteTransactionAsync(
        IAsyncSession session,
        Func<IAsyncQueryRunner, Task> work,
        Action<TransactionConfigBuilder> configure);

    protected override async Task<IProtocolMessage> ExecuteAsync(T message)
    {
        var sessionId = message.Session.Id;

        await ExecuteTransactionAsync(
            message.Session.Object,
            runner => RunAttemptAsync(runner, sessionId),
            _transactionConfigMapper.Map(message.TxMeta, message.Timeout));

        return new RetryableDoneResponse();
    }

    private async Task RunAttemptAsync(IAsyncQueryRunner runner, string sessionId)
    {
        var stored = _objectStore.Store((IAsyncTransaction)runner);
        var outcomeTask = _coordinator.WaitForOutcomeAsync(sessionId);
        _coordinator.CompleteNextResponse(new RetryableTryResponse(stored.Id));
        await outcomeTask;
    }
}
