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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver.Tests.TestBackend;

internal class NewAuthTokenManager : IProtocolObject, IAuthTokenManager
{
    private Controller _controller;
    public object data { get; set; }

    public override Task Process(Controller controller)
    {
        _controller = controller;
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("AuthTokenManager", uniqueId).Encode();
    }

    public async Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        await _controller.SendResponse(GetAuthRequest(requestId)).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<AuthTokenManagerGetAuthCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId == requestId)
        {
            return new AuthToken(result.data.auth.data.ToDictionary());
        }

        throw new Exception("GetTokenAsync: request IDs did not match");
    }

    private string GetAuthRequest(string requestId)
    {
        return new ProtocolResponse(
            "AuthTokenManagerGetAuthRequest",
            new { authTokenManagerId = uniqueId, id = requestId }).Encode();
    }

    public async Task OnTokenExpiredAsync(IAuthToken token, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        await _controller.SendResponse(GetAuthExpiredRequest(requestId, token)).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<AuthTokenManagerOnAuthExpiredCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId != requestId)
        {
            throw new Exception("OnTokenExpiredAsync: request IDs did not match");
        }
    }

    private string GetAuthExpiredRequest(string requestId, IAuthToken token)
    {
        if (token is not AuthToken authToken)
        {
            return null;
        }

        var content = authToken.Content;

        return new ProtocolResponse(
            "AuthTokenManagerOnAuthExpiredRequest",
            new
            {
                authTokenManagerId = uniqueId,
                id = requestId,
                auth = new
                {
                    name = "AuthorizationToken",
                    data = content
                }
            }).Encode();
    }
}
