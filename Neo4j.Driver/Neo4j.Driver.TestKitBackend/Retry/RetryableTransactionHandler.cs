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
// which driver transaction-function API to run. The flow runs detached from the message loop:
// ProcessAsync returns as soon as the first attempt has sent RetryableTry, and every later
// pause/resume goes through IRetryCoordinator's continuations rather than loop re-entry.
internal abstract class RetryableTransactionHandler<T> : MessageHandler<T>
    where T : IProtocolMessage, IRetryableTransactionRequest
{
    private readonly IRegistry _registry;
    private readonly IRetryCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;
    private readonly IDriverErrorMapper _driverErrorMapper;
    private readonly ILogger _logger;

    protected RetryableTransactionHandler(
        IRegistry registry,
        IRetryCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
    {
        _registry = registry;
        _coordinator = coordinator;
        _responseWriter = responseWriter;
        _driverErrorMapper = driverErrorMapper;
        _logger = logger;
    }

    protected abstract Task ExecuteTransactionAsync(IAsyncSession session, Func<IAsyncQueryRunner, Task> work);

    public override async Task ProcessAsync(T message)
    {
        var sessionId = message.Session.Id;
        var responseTask = _coordinator.WaitForNextResponseAsync(sessionId);
        _ = RunFlowAsync(message.Session.Object, sessionId);
        await _responseWriter.WriteAsync(await responseTask);
    }

    private async Task RunFlowAsync(IAsyncSession session, string sessionId)
    {
        try
        {
            await ExecuteTransactionAsync(session, runner => RunAttemptAsync(runner, sessionId));
            _coordinator.CompleteNextResponse(sessionId, new RetryableDoneResponse());
        }
        catch (FrontendException exception)
        {
            _coordinator.CompleteNextResponse(sessionId, new FrontendErrorResponse { Msg = exception.Message });
        }
        catch (Neo4jException exception)
        {
            _logger.LogDebug(exception, "Retryable transaction for session '{SessionId}' failed", sessionId);
            _coordinator.CompleteNextResponse(sessionId, _driverErrorMapper.Map(exception));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled error during retryable transaction for session '{SessionId}'",
                sessionId);

            _coordinator.CompleteNextResponse(sessionId, new BackendErrorResponse { Msg = exception.Message });
        }
    }

    private async Task RunAttemptAsync(IAsyncQueryRunner runner, string sessionId)
    {
        var registered = _registry.Register((IAsyncTransaction)runner);
        var outcomeTask = _coordinator.WaitForOutcomeAsync(sessionId);
        _coordinator.CompleteNextResponse(sessionId, new RetryableTryResponse(registered.Id));
        await outcomeTask;
    }
}
