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

using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record BasicAuthTokenProviderRequest(string BasicAuthTokenManagerId) : ICorrelatedRequest
{
    public string Id { get; set; } = "";
}

internal record BasicAuthTokenProviderCompleted : IProtocolMessage
{
    public required string RequestId { get; init; }
    public required IWireType<AuthorizationToken> Auth { get; init; }
}

internal class BasicAuthTokenProviderCompletedHandler : MessageHandler<BasicAuthTokenProviderCompleted>
{
    private readonly IExpectationStore _expectationStore;

    public BasicAuthTokenProviderCompletedHandler(IExpectationStore expectationStore)
    {
        _expectationStore = expectationStore;
    }

    public override Task ProcessAsync(BasicAuthTokenProviderCompleted message)
    {
        _expectationStore.Fulfil(message.RequestId, message.Auth.Value.ToAuthToken());
        return Task.CompletedTask;
    }
}
