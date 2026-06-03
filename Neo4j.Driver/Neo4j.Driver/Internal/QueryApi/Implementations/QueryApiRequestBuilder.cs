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

using System.Collections.Generic;
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
    private const string TypedJsonMediaType = "application/vnd.neo4j.query.v1.1";
    
    private readonly IEnumerable<IHttpRequestEnricher> _requestEnrichers;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ISessionContext _sessionContext;
    private readonly IQueryApiUrlBuilder _urlBuilder;

    public QueryApiRequestBuilder(
        IQueryApiUrlBuilder urlBuilder,
        ISessionContext sessionContext,
        IEnumerable<IHttpRequestEnricher> requestEnrichers,
        IJsonSerializer jsonSerializer)
    {
        _urlBuilder = urlBuilder;
        _sessionContext = sessionContext;
        _requestEnrichers = requestEnrichers;
        _jsonSerializer = jsonSerializer;
    }

    public Task<HttpRequestMessage> PostAsync(string path, object? body, CancellationToken cancellationToken = default)
    {
        return BuildAsync(HttpMethod.Post, path, body ?? new object(), cancellationToken);
    }

    public Task<HttpRequestMessage> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        return BuildAsync(HttpMethod.Delete, path, null, cancellationToken);
    }

    private async Task<HttpRequestMessage> BuildAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, _urlBuilder.Build($"db/{_sessionContext.Database}/{path}"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(TypedJsonMediaType));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));
        foreach (var enricher in _requestEnrichers)
        {
            await enricher.Enrich(request, cancellationToken).ConfigureAwait(false);
        }

        if (body is not null)
        {
            request.Content = _jsonSerializer.Serialize(body);
        }

        return request;
    }
}
