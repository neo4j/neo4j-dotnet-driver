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
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class QueryApiHttpTransport : IQueryApiHttpTransport
{
    private readonly HttpClient _client;
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly ILogger _logger;

    public QueryApiHttpTransport(IQueryApiErrorChecker errorChecker, ILogger logger)
    {
        _errorChecker = errorChecker;
        _logger = logger;
        _client = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) });
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Sending {method} request to {uri}", request.Method, request.RequestUri);
        var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.Debug(
            "{method} {uri} returned {statusCode}", request.Method, request.RequestUri, (int)response.StatusCode);

        await _errorChecker.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
