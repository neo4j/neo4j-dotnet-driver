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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Auth;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver.Tests.TestBackend;

internal abstract class TestAuthTokenManager : IProtocolObject, IAuthTokenManager
{
    public abstract Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default);
    public abstract Task OnTokenExpiredAsync(IAuthToken token, CancellationToken cancellationToken = default);
}

internal class NewExpirationBasedAuthTokenManager : IProtocolObject
{
    protected Controller _controller;
    public ExpirationBasedAuthTokenManager tokenManager;
    public object data { get; set; }
    
    public override Task Process(Controller controller)
    {
        _controller = controller;
        tokenManager = new ExpirationBasedAuthTokenManager(GetTokenAsync);
        return Task.CompletedTask;
    }

    public async Task<AuthTokenAndExpiration> GetTokenAsync()
    {
        var requestId = Guid.NewGuid().ToString();
        await _controller.SendResponse(GetAuthRequest(requestId)).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<ExpirationBasedAuthTokenProviderCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId == requestId)
        {
            return new AuthTokenAndExpiration(
                new AuthToken(result.data.auth.data.auth.data.ToDictionary()),
                DateTime.Now.AddMilliseconds(result.data.auth.data.expiresInMs));
        }

        throw new Exception("GetTokenAsync: request IDs did not match");
    }

    public override string Respond()
    {
        return new ProtocolResponse("ExpirationBasedAuthTokenManager", uniqueId).Encode();
    }
    
    protected string GetAuthRequest(string requestId)
    {
        return new ProtocolResponse(
            "ExpirationBasedAuthTokenProviderRequest",
            new { expirationBasedAuthTokenManagerId = uniqueId, id = requestId }).Encode();
    }
}
