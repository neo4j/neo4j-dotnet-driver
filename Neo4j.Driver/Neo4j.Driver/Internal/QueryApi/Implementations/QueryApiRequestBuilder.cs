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

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiRequestBuilder : IQueryApiRequestBuilder
{
    private readonly IAuthApplicator _authApplicator;
    private readonly IClusterAffinityApplicator _clusterAffinityApplicator;
    private readonly IQueryApiUrlBuilder _urlBuilder;

    public QueryApiRequestBuilder(
        IQueryApiUrlBuilder urlBuilder,
        IAuthApplicator authApplicator,
        IClusterAffinityApplicator clusterAffinityApplicator)
    {
        _urlBuilder = urlBuilder;
        _authApplicator = authApplicator;
        _clusterAffinityApplicator = clusterAffinityApplicator;
    }

    public Uri BaseUri => _urlBuilder.Build(string.Empty);

    public HttpRequestMessage Post(string path, IAuthToken auth, QueryApiTransactionContext? txContext = null)
    {
        return Build(HttpMethod.Post, path, auth, txContext);
    }

    public HttpRequestMessage Delete(string path, IAuthToken auth, QueryApiTransactionContext? txContext = null)
    {
        return Build(HttpMethod.Delete, path, auth, txContext);
    }

    private HttpRequestMessage Build(
        HttpMethod method,
        string path,
        IAuthToken auth,
        QueryApiTransactionContext? txContext)
    {
        var request = new HttpRequestMessage(method, _urlBuilder.Build(path));
        _authApplicator.Apply(request, auth);
        if (txContext is not null)
        {
            _clusterAffinityApplicator.Apply(request, txContext);
        }

        return request;
    }
}
