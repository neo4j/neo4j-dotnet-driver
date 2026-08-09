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
using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record RetryableNegativeRequest(RegistryObject<IAsyncSession> Session, string ErrorId) : IProtocolMessage;

internal class RetryableNegativeHandler : MessageHandler<RetryableNegativeRequest>
{
    private readonly IRegistry _registry;
    private readonly IContinuationCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;

    public RetryableNegativeHandler(
        IRegistry registry,
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter)
    {
        _registry = registry;
        _coordinator = coordinator;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(RetryableNegativeRequest message)
    {
        var sessionId = message.Session.Id;
        var responseTask = _coordinator.WaitForNextResponseAsync();

        try
        {
            var exception = message.ErrorId == ""
                ? new FrontendException("Error from client in retryable tx")
                : _registry.Get<Exception>(message.ErrorId).Object;

            _coordinator.FailOutcome(sessionId, exception);
        }
        catch
        {
            _coordinator.CancelNextResponse();
            throw;
        }

        await _responseWriter.WriteAsync(await responseTask);
    }
}
