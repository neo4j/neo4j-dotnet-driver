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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Retry;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record SessionReadTransactionRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncSession> Session { get; init; }

    // Cypher-envelope dict on the wire; parsed but not yet converted to native values (M11).
    public Dictionary<string, JsonElement>? TxMeta { get; init; }

    // Absent = driver default, null = explicitly no timeout, number = timeout in ms.
    public Optional<long?> Timeout { get; init; }
}

internal record RetryableTryResponse(string Id) : IProtocolMessage;

internal record RetryableDoneResponse : IProtocolMessage;

internal class SessionReadTransactionHandler : MessageHandler<SessionReadTransactionRequest>
{
    private readonly IRegistry _registry;
    private readonly IRetryCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public SessionReadTransactionHandler(
        IRegistry registry,
        IRetryCoordinator coordinator,
        IResponseWriter responseWriter,
        ILogger logger)
    {
        _registry = registry;
        _coordinator = coordinator;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(SessionReadTransactionRequest message)
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
            await session.ExecuteReadAsync(runner => RunAttemptAsync(runner, sessionId));
            _coordinator.CompleteNextResponse(sessionId, new RetryableDoneResponse());
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled error during retryable read transaction for session '{SessionId}'",
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
