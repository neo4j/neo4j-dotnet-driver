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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Retry;

internal interface IRetryableTransactionRequest
{
    RegistryObject<IAsyncSession> Session { get; }
}

// Shared flow for SessionReadTransaction/SessionWriteTransaction (spec §7); subclasses supply
// which driver transaction-function API to run. Each attempt pauses after sending RetryableTry
// and resumes when RetryablePositive/RetryableNegative resolves its outcome.
internal abstract class RetryableTransactionHandler<T> : DetachedOperationHandler<T>
    where T : IProtocolMessage, IRetryableTransactionRequest
{
    private readonly IRegistry _registry;
    private readonly IContinuationCoordinator _coordinator;

    protected RetryableTransactionHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
        : base(coordinator, responseWriter, driverErrorMapper, logger)
    {
        _registry = registry;
        _coordinator = coordinator;
    }

    protected abstract Task ExecuteTransactionAsync(IAsyncSession session, Func<IAsyncQueryRunner, Task> work);

    protected override async Task<IProtocolMessage> ExecuteAsync(T message)
    {
        var sessionId = message.Session.Id;
        await ExecuteTransactionAsync(message.Session.Object, runner => RunAttemptAsync(runner, sessionId));
        return new RetryableDoneResponse();
    }

    private async Task RunAttemptAsync(IAsyncQueryRunner runner, string sessionId)
    {
        var registered = _registry.Register((IAsyncTransaction)runner);
        var outcomeTask = _coordinator.WaitForOutcomeAsync(sessionId);
        _coordinator.CompleteNextResponse(new RetryableTryResponse(registered.Id));
        await outcomeTask;
    }
}
