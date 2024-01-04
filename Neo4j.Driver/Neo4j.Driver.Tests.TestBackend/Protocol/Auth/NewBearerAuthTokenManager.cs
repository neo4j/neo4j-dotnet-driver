// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
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

using System;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Tests.TestBackend.Protocol.Time;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Auth;

internal class NewBearerAuthTokenManager : NewNeo4jAuthTokenManager
{
    public object data { get; set; }
    
    public override Task Process(Controller controller)
    {
        _controller = controller;
        TokenManager = AuthTokenManagers.Bearer(FakeTime.Instance, GetTokenAsync);
        return Task.CompletedTask;
    }

    public async ValueTask<AuthTokenAndExpiration> GetTokenAsync()
    {
        var requestId = Guid.NewGuid().ToString();
        await _controller.SendResponse(GetAuthRequest(requestId)).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<BearerAuthTokenProviderCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId == requestId)
        {
            var token = new AuthToken(result.data.auth.data.auth.data.ToDictionary());
            var expiresInMs = result.data.auth.data.expiresInMs;
            var expiry = expiresInMs == 0
                ? DateTime.MaxValue
                : FakeTime.Instance.Now().AddMilliseconds(expiresInMs);

            return new AuthTokenAndExpiration(token, expiry);
        }

        throw new Exception("GetTokenAsync: request IDs did not match");
    }

    public override string Respond()
    {
        return new ProtocolResponse("BearerAuthTokenManager", uniqueId).Encode();
    }
    
    protected string GetAuthRequest(string requestId)
    {
        return new ProtocolResponse(
            "BearerAuthTokenProviderRequest",
            new { bearerAuthTokenManagerId = uniqueId, id = requestId }).Encode();
    }
}
