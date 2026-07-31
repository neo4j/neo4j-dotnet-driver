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

using Neo4j.Driver.TestKitBackend.Continuations;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

// Backend → testkit callback (spec §6): the driver needs a fresh bearer token. Sent in place
// of the open request's response; Id is the correlation token testkit echoes back.
internal record BearerAuthTokenProviderRequest(string Id, string BearerAuthTokenManagerId) : IProtocolMessage;

// Testkit's token-plus-expiry payload; expiresInMs null/absent = the token never expires.
// Shares its simple name with the driver's public type — the driver one is aliased where both
// are needed.
internal record AuthTokenAndExpiration(
    IWireType<AuthorizationToken> Auth,
    long? ExpiresInMs = null) : IWireType<AuthTokenAndExpiration>;

internal record BearerAuthTokenProviderCompletedRequest : ICallbackCompletion
{
    public required string RequestId { get; init; }
    public required IWireType<AuthTokenAndExpiration> Auth { get; init; }
}
