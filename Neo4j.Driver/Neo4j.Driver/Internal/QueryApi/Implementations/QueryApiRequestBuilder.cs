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

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiRequestBuilder : IQueryApiRequestBuilder
{
    private readonly IAuthApplicator _authApplicator;
    private readonly IClusterAffinityApplicator _clusterAffinityApplicator;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ISessionContext _sessionContext;
    private readonly IQueryApiUrlBuilder _urlBuilder;

    public QueryApiRequestBuilder(
        IQueryApiUrlBuilder urlBuilder,
        ISessionContext sessionContext,
        IAuthApplicator authApplicator,
        IClusterAffinityApplicator clusterAffinityApplicator,
        IJsonSerializer jsonSerializer)
    {
        _urlBuilder = urlBuilder;
        _sessionContext = sessionContext;
        _authApplicator = authApplicator;
        _clusterAffinityApplicator = clusterAffinityApplicator;
        _jsonSerializer = jsonSerializer;
    }

    public Task<HttpRequestMessage> PostAsync(string path, object? body, CancellationToken cancellationToken = default)
    {
        // POST requests must have *some* body, even if it's empty
        var theBody = body ?? new object();
        return BuildAsync(HttpMethod.Post, path, theBody, cancellationToken);
    }

    public Task<HttpRequestMessage> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        return BuildAsync(HttpMethod.Delete, path, null, cancellationToken);
    }

    private const string TypedJsonMediaType = "application/vnd.neo4j.query.v1.1";

    private async Task<HttpRequestMessage> BuildAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, _urlBuilder.Build($"db/{_sessionContext.Database}/{path}"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(TypedJsonMediaType));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));
        var auth = await _sessionContext.GetAuthTokenAsync(cancellationToken).ConfigureAwait(false);
        _authApplicator.Apply(request, auth);
        _clusterAffinityApplicator.Apply(request);

        if (body is not null)
        {
            request.Content = _jsonSerializer.Serialize(body);
        }

        return request;
    }
}
