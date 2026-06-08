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
    private readonly IBase64Encoder _encoder;

    public QueryApiAuthEnricher(IAuthTokenManager authTokenManager, IBase64Encoder encoder)
    {
        _authTokenManager = authTokenManager;
        _encoder = encoder;
    }

    private AuthenticationHeaderValue BuildBasicHeader(AuthToken authToken)
    {
        var principal = authToken.Principal ?? string.Empty;
        var credentials = authToken.Content.GetValueOrDefault(AuthToken.CredentialsKey) as string  ?? string.Empty;

        if (principal.Contains(':'))
        {
            throw new NotSupportedException(
                "Basic auth principal (username) must not contain a colon (':') — " +
                "the Query API cannot distinguish it from the credentials separator.");
        }

        return authToken.Realm is null 
            ? new AuthenticationHeaderValue("Basic", _encoder.Encode($"{principal}:{credentials}")) 
            : throw new NotSupportedException("Basic auth realm is not supported by the Query API.");
    }

    private AuthenticationHeaderValue BuildBearerHeader(AuthToken authToken)
    {
        var token = authToken.Content.GetValueOrDefault(AuthToken.CredentialsKey) as string  ?? string.Empty;
        return new AuthenticationHeaderValue("Bearer", _encoder.Encode(token));
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
            "basic" => BuildBasicHeader(authToken),
            "bearer" => BuildBearerHeader(authToken),
            "none" => null,
            _  => throw new NotSupportedException($"Auth scheme '{authToken.Scheme}' is not supported by the Query API.")
        };
    }
}
