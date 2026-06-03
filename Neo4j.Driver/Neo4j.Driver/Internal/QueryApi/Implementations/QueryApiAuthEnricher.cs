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

#nullable enable

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiAuthEnricher : IHttpRequestEnricher
{
    private readonly IAuthTokenManager _authTokenManager;

    public QueryApiAuthEnricher(IAuthTokenManager authTokenManager)
    {
        _authTokenManager = authTokenManager;
    }

    public async ValueTask Enrich(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var returnedToken = await _authTokenManager.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (returnedToken is not AuthToken authToken)
        {
            throw new InvalidOperationException("Unsupported auth token: " + returnedToken.GetType().Name);
        }
        
        request.Headers.Authorization = authToken.Scheme switch
        {
            "basic" => new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{authToken.Principal}:{authToken.Content.GetValueOrDefault(AuthToken.CredentialsKey)}"))),

            "bearer" => new AuthenticationHeaderValue(
                "Bearer",
                authToken.Content.GetValueOrDefault(AuthToken.CredentialsKey) as string),

            "none" => null,

            _ => throw new NotSupportedException($"Auth scheme '{authToken.Scheme}' is not supported by the Query API.")
        };
    }
}
