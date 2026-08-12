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

using Neo4j.Driver.Internal.Services;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Serialization;
using DriverAuthTokenAndExpiration = Neo4j.Driver.AuthTokenAndExpiration;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record BearerAuthTokenProviderRequest(string BearerAuthTokenManagerId) : ICorrelatedRequest
{
    public string Id { get; set; } = "";
}

// Testkit's token-plus-expiry payload; expiresInMs null/absent = the token never expires.
internal record AuthTokenAndExpiration(
    IWireType<AuthorizationToken> Auth,
    long? ExpiresInMs = null) : IWireType<AuthTokenAndExpiration>;

internal record BearerAuthTokenProviderCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required IWireType<AuthTokenAndExpiration> Auth { get; init; }
}

internal class BearerAuthTokenProviderCompletedHandler : MessageHandler<BearerAuthTokenProviderCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public BearerAuthTokenProviderCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(BearerAuthTokenProviderCompleted message)
    {
        var payload = message.Auth.Value;
        var token = payload.Auth.Value.ToAuthToken();
        var domainValue = payload.ExpiresInMs is { } expiresInMs
            ? new DriverAuthTokenAndExpiration(token, DateTimeProvider.StaticInstance.Now().AddMilliseconds(expiresInMs))
            : new DriverAuthTokenAndExpiration(token);

        _expectationStore.Fulfil(message.RequestId, domainValue);
        return Task.CompletedTask;
    }
}
