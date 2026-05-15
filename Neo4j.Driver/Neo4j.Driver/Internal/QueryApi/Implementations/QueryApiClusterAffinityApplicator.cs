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
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiClusterAffinityApplicator : IClusterAffinityApplicator
{
    private const string HeaderName = "neo4j-cluster-affinity";

    private readonly IScoped<QueryApiTransactionContext> _txContext;

    public QueryApiClusterAffinityApplicator(IScoped<QueryApiTransactionContext> txContext)
    {
        _txContext = txContext;
    }

    public void Apply(HttpRequestMessage request)
    {
        if (_txContext.TryGetValue(out var ctx) && ctx!.ClusterAffinity is not null)
        {
            request.Headers.TryAddWithoutValidation(HeaderName, ctx.ClusterAffinity);
        }
    }

    public string? Extract(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues(HeaderName, out var vals)
            ? string.Join(",", vals)
            : null;
    }
}
