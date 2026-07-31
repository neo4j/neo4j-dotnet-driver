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
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

// Backend → testkit callback (spec §6): the driver asked the custom manager for its token. Sent
// in place of the open request's response; Id is the correlation token testkit echoes back.
internal record AuthTokenManagerGetAuthRequest(string Id, string AuthTokenManagerId) : IProtocolMessage;

internal record AuthTokenManagerGetAuthCompletedRequest : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required IWireType<AuthorizationToken> Auth { get; init; }
}

// No direct response of its own — the reply is whatever the resumed driver operation produces
// next (its terminal response or another callback request), per spec §6.
internal class AuthTokenManagerGetAuthCompletedHandler : MessageHandler<AuthTokenManagerGetAuthCompletedRequest>
{
    private readonly IContinuationCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;

    public AuthTokenManagerGetAuthCompletedHandler(
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter)
    {
        _coordinator = coordinator;
        _responseWriter = responseWriter;
    }

    public override async Task ProcessAsync(AuthTokenManagerGetAuthCompletedRequest message)
    {
        var responseTask = _coordinator.WaitForNextResponseAsync();
        _coordinator.CompleteCallback(message.RequestId, message);
        await _responseWriter.WriteAsync(await responseTask);
    }
}
