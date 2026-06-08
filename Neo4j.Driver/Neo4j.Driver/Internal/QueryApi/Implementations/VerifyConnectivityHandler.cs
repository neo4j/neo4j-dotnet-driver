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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

/// <summary>
/// Verifies connectivity by hitting the Neo4j discovery endpoint (<c>GET /</c>) and confirming that the response
/// advertises both the Query API endpoint and the server version. Spec: https://neo4j.com/docs/http-api/current/discovery/
/// Decision: always hit discovery even when the driver is warm; do not run a dummy query.
/// </summary>
[AutoRegister]
internal class VerifyConnectivityHandler : IVerifyConnectivityHandler
{
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILogger _logger;
    private readonly QueryApiServerInfo _serverInfo;
    private readonly IQueryApiUrlBuilder _urlBuilder;

    public VerifyConnectivityHandler(
        IQueryApiUrlBuilder urlBuilder,
        IQueryApiHttpClient httpClient,
        IJsonDeserializer jsonDeserializer,
        QueryApiServerInfo serverInfo,
        ILogger logger)
    {
        _urlBuilder = urlBuilder;
        _httpClient = httpClient;
        _jsonDeserializer = jsonDeserializer;
        _serverInfo = serverInfo;
        _logger = logger;
    }

    public async Task<IServerInfo> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        _logger.Debug("Verifying connectivity via discovery endpoint at {address}", _serverInfo.Address);
        using var request = new HttpRequestMessage(HttpMethod.Get, _urlBuilder.Build(string.Empty));
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var body = await _jsonDeserializer
            .DeserializeAsync<DiscoveryResponse>(responseContent, JsonNamingPolicy.SnakeCaseLower, cancellationToken)
            .ConfigureAwait(false);

        if (body?.Query is null)
        {
            throw new ServiceUnavailableException(
                "The discovery endpoint did not advertise a Query API endpoint. " +
                "Ensure the server supports the Query API (Neo4j 5.x+).");
        }

        if (body.Neo4jVersion is null)
        {
            throw new ServiceUnavailableException("The discovery endpoint did not include a server version.");
        }

        _serverInfo.UpdateAgent(body.Neo4jVersion);
        _logger.Debug("Connectivity verified; server version {version}", body.Neo4jVersion);
        return _serverInfo;
    }

    internal class DiscoveryResponse
    {
        public string? Query { get; init; }
        public string? Neo4jVersion { get; init; }
    }
}
