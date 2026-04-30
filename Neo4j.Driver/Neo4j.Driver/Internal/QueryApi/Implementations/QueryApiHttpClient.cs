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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal class QueryApiHttpClient : IQueryApiHttpClient
{
    private readonly HttpClient _client;

    /// <param name="handlerFactory">
    /// Optional factory for a custom <see cref="HttpMessageHandler"/>. When <c>null</c>, a
    /// <see cref="SocketsHttpHandler"/> with a two-minute <see cref="SocketsHttpHandler.PooledConnectionLifetime"/> is used,
    /// giving DNS rotation without requiring a DI container.
    /// </param>
    public QueryApiHttpClient(Func<HttpMessageHandler>? handlerFactory = null)
    {
        var handler = handlerFactory?.Invoke() ??
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            };

        _client = new HttpClient(handler);
    }

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        return _client.SendAsync(request, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
