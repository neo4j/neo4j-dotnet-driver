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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.QueryApi;

internal class VerifyConnectivityHandler : IVerifyConnectivityHandler
{
    private readonly IQueryApiUrlBuilder _urlBuilder;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAuthApplicator _authApplicator;

    public VerifyConnectivityHandler(
        IQueryApiUrlBuilder urlBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        JsonSerializerOptions jsonOptions,
        IAuthApplicator authApplicator)
    {
        _urlBuilder = urlBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonOptions = jsonOptions;
        _authApplicator = authApplicator;
    }

    public async Task<IServerInfo> VerifyConnectivityAsync(
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(auth);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await _errorChecker.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var url = _urlBuilder.Build(string.Empty);
        var address = $"{url.Host}:{url.Port}";
        var agent = response.Headers.Server?.ToString() ?? string.Empty;
        return new ServerInfo(address, "QueryApi/2.0", agent);
    }

    private HttpRequestMessage BuildRequest(IAuthToken auth)
    {
        var body = new RequestBody { Statement = "RETURN 1" };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            _urlBuilder.Build("db/system/query/v2"));

        _authApplicator.Apply(request, auth);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json");

        return request;
    }

    private class RequestBody
    {
        public string? Statement { get; init; }
    }

    private class ServerInfo : IServerInfo
    {
        public ServerInfo(string address, string protocolVersion, string agent)
        {
            Address = address;
            ProtocolVersion = protocolVersion;
            Agent = agent;
        }

        public string Address { get; }
        public string ProtocolVersion { get; }
        public string Agent { get; }
    }
}
