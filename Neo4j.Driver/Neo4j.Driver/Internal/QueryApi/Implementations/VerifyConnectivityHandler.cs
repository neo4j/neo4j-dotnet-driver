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

namespace Neo4j.Driver.Internal.QueryApi;

internal class VerifyConnectivityHandler : IVerifyConnectivityHandler
{
    private readonly IQueryApiErrorChecker _errorChecker;
    private readonly IQueryApiHttpClient _httpClient;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IQueryApiRequestBuilder _requestBuilder;

    public VerifyConnectivityHandler(
        IQueryApiRequestBuilder requestBuilder,
        IQueryApiHttpClient httpClient,
        IQueryApiErrorChecker errorChecker,
        IJsonSerializer jsonSerializer)
    {
        _requestBuilder = requestBuilder;
        _httpClient = httpClient;
        _errorChecker = errorChecker;
        _jsonSerializer = jsonSerializer;
    }

    public async Task<IServerInfo> VerifyConnectivityAsync(
        IAuthToken auth,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(auth);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await _errorChecker.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var baseUri = _requestBuilder.BaseUri;
        var address = $"{baseUri.Host}:{baseUri.Port}";
        var agent = response.Headers.Server?.ToString() ?? string.Empty;
        return new ServerInfo(address, "QueryApi/2.0", agent);
    }

    private HttpRequestMessage BuildRequest(IAuthToken auth)
    {
        var body = new RequestBody { Statement = "RETURN 1" };
        var request = _requestBuilder.Post("db/system/query/v2", auth);
        request.Content = _jsonSerializer.Serialize(body);
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
