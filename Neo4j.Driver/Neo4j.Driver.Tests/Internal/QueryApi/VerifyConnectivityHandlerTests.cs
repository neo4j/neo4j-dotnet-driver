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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiTestHelpers;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Connectivity verification hits the Neo4j discovery endpoint (<c>GET /</c>) and confirms that the server
/// advertises both the Query API endpoint and a server version. No dummy query is run. Decision ref:
/// https://neo4j.com/docs/http-api/current/discovery/
/// </summary>
public class VerifyConnectivityHandlerTests
{
    private static AutoMocker CreateMocker(FakeQueryApiHttpClient httpClient)
    {
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<IQueryApiUrlBuilder>(new QueryApiUrlBuilder(BaseUri));
        mocker.Use<IJsonDeserializer>(new QueryApiJsonSerializer());
        return mocker;
    }

    private static HttpResponseMessage DiscoveryResponse(
        string? queryEndpoint = "http://localhost:7474/query/v2",
        string? neo4jVersion = "5.18.0")
    {
        return OkWith(new { query = queryEndpoint, neo4jVersion });
    }

    [Fact]
    public async Task SendsGet_ToDiscoveryEndpoint()
    {
        // Always hits GET / unconditionally — even when the driver is warm
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse());
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        await handler.VerifyConnectivityAsync();

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/");
    }

    [Fact]
    public async Task DoesNotSendAuthorizationHeader_OnDiscoveryRequest()
    {
        // Discovery endpoint is unauthenticated
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse());
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        await handler.VerifyConnectivityAsync();

        httpClient.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsServerInfo_WithHostAndPort_FromBaseUri()
    {
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse());
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        var serverInfo = await handler.VerifyConnectivityAsync();

        serverInfo.Address.Should().Be("localhost:7474");
    }

    [Fact]
    public async Task ReturnsServerInfo_WithVersion_FromDiscoveryBody()
    {
        // neo4jVersion from discovery response is surfaced as IServerInfo.Agent
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse(neo4jVersion: "5.22.0"));
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        var serverInfo = await handler.VerifyConnectivityAsync();

        serverInfo.Agent.Should().Be("5.22.0");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenQueryApiEndpointAbsentFromDiscovery()
    {
        // Server is running but does not support the Query API (e.g. Neo4j 4.x)
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse(null));
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        var act = () => handler.VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*Query API endpoint*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenServerVersionAbsentFromDiscovery()
    {
        var httpClient = new FakeQueryApiHttpClient(DiscoveryResponse(neo4jVersion: null));
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        var act = () => handler.VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*server version*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenDiscoveryEndpointReturnsNon2xx()
    {
        var httpClient = new FakeQueryApiHttpClient(new HttpResponseMessage(HttpStatusCode.NotFound));
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        var act = () => handler.VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*404*");
    }
}
