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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Connectivity verification hits the Neo4j discovery endpoint (<c>GET /</c>) and confirms that the server
/// advertises both the Query API endpoint and a server version. No dummy query is run. Decision ref:
/// https://neo4j.com/docs/http-api/current/discovery/
/// </summary>
public class VerifyConnectivityHandlerTests
{
    private static readonly Uri BaseUri = new("https://neo4j.example.com:7474/");

    /// <summary>
    /// Minimum chain: Build("") → discoveryUri → SendAsync(GET discoveryUri) → response.
    /// DeserializeAsync defaults to null (safe — handler throws before relying on content for non-2xx).
    /// </summary>
    private static AutoMocker CreateChain(
        out HttpResponseMessage response,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mocker = new AutoMocker();
        mocker.Use(new QueryApiServerInfo(BaseUri));
        response = new HttpResponseMessage(statusCode) { Content = new ByteArrayContent([]) };

        mocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build(string.Empty))
            .Returns(BaseUri);

        // The handler builds its own request internally, so we constrain on GET method rather than instance identity
        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get), default))
            .ReturnsAsync(response);

        return mocker;
    }

    private static VerifyConnectivityHandler.DiscoveryResponse ValidDiscovery(
        string? query = "http://localhost:7474/query/v2",
        string? neo4jVersion = "5.18.0")
    {
        return new VerifyConnectivityHandler.DiscoveryResponse { Query = query, Neo4jVersion = neo4jVersion };
    }

    [Fact]
    public async Task SendsGet_ToDiscoveryEndpoint()
    {
        // Always hits GET / unconditionally — even when the driver is warm
        HttpRequestMessage? capturedRequest = null;
        var mocker = new AutoMocker();
        mocker.Use(new QueryApiServerInfo(BaseUri));
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        mocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build(string.Empty))
            .Returns(BaseUri);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), default))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidDiscovery());

        await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        capturedRequest!.Method.Should().Be(HttpMethod.Get);
        capturedRequest.RequestUri.Should().Be(BaseUri);
    }

    [Fact]
    public async Task DoesNotSendAuthorizationHeader_OnDiscoveryRequest()
    {
        // Discovery endpoint is unauthenticated — the handler must not add any Authorization header
        HttpRequestMessage? capturedRequest = null;
        var mocker = new AutoMocker();
        mocker.Use(new QueryApiServerInfo(BaseUri));
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        mocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build(string.Empty))
            .Returns(BaseUri);

        mocker.GetMock<IQueryApiHttpClient>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), default))
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidDiscovery());

        await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsServerInfo_WithHostAndPort_FromBaseUri()
    {
        // Address comes from the IQueryApiUrlBuilder, not the discovery body
        var mocker = CreateChain(out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidDiscovery());

        var serverInfo = await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        serverInfo.Address.Should().Be("neo4j.example.com:7474");
    }

    [Fact]
    public async Task ReturnsServerInfo_WithVersion_FromDiscoveryBody()
    {
        // neo4jVersion from the discovery response surfaces as IServerInfo.Agent
        var mocker = CreateChain(out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidDiscovery(neo4jVersion: "5.22.0"));

        var serverInfo = await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        serverInfo.Agent.Should().Be("5.22.0");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenQueryApiEndpointAbsentFromDiscovery()
    {
        // Server is running but does not support the Query API (e.g. Neo4j 4.x)
        var mocker = CreateChain(out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(It.IsAny<Stream>(), default))
            .ReturnsAsync(ValidDiscovery(query: null));

        var act = () => mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*Query API endpoint*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenServerVersionAbsentFromDiscovery()
    {
        var mocker = CreateChain(out _);

        mocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<VerifyConnectivityHandler.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidDiscovery(neo4jVersion: null));

        var act = () => mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*server version*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenDiscoveryEndpointReturnsNon2xx()
    {
        var mocker = CreateChain(out _, statusCode: HttpStatusCode.NotFound);

        var act = () => mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync();

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*404*");
    }
}
