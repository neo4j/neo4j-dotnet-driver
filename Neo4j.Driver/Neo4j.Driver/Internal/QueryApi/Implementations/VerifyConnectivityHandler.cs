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
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

/// <summary>
/// Verifies connectivity by hitting the Neo4j discovery endpoint (<c>GET /</c>) and confirming that the response
/// advertises both the Query API endpoint and the server version. Spec: https://neo4j.com/docs/http-api/current/discovery/
/// Decision: always hit discovery even when driver is warm; do not run a dummy query.
/// </summary>
[AutoRegister]
internal class VerifyConnectivityHandler : IVerifyConnectivityHandler
{
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IQueryApiUrlBuilder _urlBuilder;

    public VerifyConnectivityHandler(
        IQueryApiUrlBuilder urlBuilder,
        IQueryApiHttpClient httpClient,
        IJsonDeserializer jsonDeserializer)
    {
        _urlBuilder = urlBuilder;
        _httpClient = httpClient;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<IServerInfo> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _urlBuilder.Build(string.Empty));
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ServiceUnavailableException(
                $"Discovery endpoint returned HTTP {(int)response.StatusCode}. " +
                "Verify the server is running and the base URI is correct.");
        }

        var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var body = await _jsonDeserializer
            .DeserializeAsync<DiscoveryResponse>(responseContent, cancellationToken)
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

        var baseUri = _urlBuilder.Build(string.Empty);
        return new ServerInfo($"{baseUri.Host}:{baseUri.Port}", body.Neo4jVersion);
    }

    internal class DiscoveryResponse
    {
        /// The Query API base URL, e.g. http://localhost:7474/query/v2
        public string? Query { get; init; }

        public string? Neo4jVersion { get; init; }
    }

    private sealed class ServerInfo : IServerInfo
    {
        public ServerInfo(string address, string agent)
        {
            Address = address;
            Agent = agent;
            ProtocolVersion = "QueryApi/2.0";
        }

        public string Address { get; }
        public string ProtocolVersion { get; }
        public string Agent { get; }
    }
}
