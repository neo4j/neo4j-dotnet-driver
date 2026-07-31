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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Retry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record RetryableNegativeRequest : IProtocolMessage
{
    public required RegistryObject<IAsyncSession> Session { get; init; }

    // Id of a stored driver error to re-raise in the work function, or "" when the failure
    // originated in test/client code (spec §7) — plain string, not a registry-bound property,
    // because "" is a valid value that must not be looked up.
    public required string ErrorId { get; init; }
}

// No direct response of its own — the reply is whatever the backgrounded retry flow produces
// next (another RetryableTry if the driver retries, or a terminal error), per spec §7.
internal class RetryableNegativeHandler : MessageHandler<RetryableNegativeRequest>
{
    private readonly IRegistry _registry;
    private readonly IRetryCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;

    public RetryableNegativeHandler(
        IRegistry registry,
        IRetryCoordinator coordinator,
        IResponseWriter responseWriter)
    {
        _registry = registry;
        _coordinator = coordinator;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(RetryableNegativeRequest message)
    {
        var sessionId = message.Session.Id;
        var responseTask = _coordinator.WaitForNextResponseAsync(sessionId);

        // The work function rethrows this inside the driver's retry loop, so the driver itself
        // decides what happens next: a retryable stored error means another attempt (and another
        // RetryableTry); anything else propagates as the flow's terminal error.
        var exception = message.ErrorId == ""
            ? new FrontendException("Error from client in retryable tx")
            : _registry.Get<Exception>(message.ErrorId).Object;

        _coordinator.FailOutcome(sessionId, exception);
        await _responseWriter.WriteAsync(await responseTask);
    }
}
