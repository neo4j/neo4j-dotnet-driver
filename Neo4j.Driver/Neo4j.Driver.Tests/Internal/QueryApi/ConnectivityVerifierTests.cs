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
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Connectivity verification hits the Neo4j discovery endpoint (<c>GET /</c>) and confirms that the server
/// advertises both the Query API endpoint and a server version. No dummy query is run. Decision ref:
/// https://neo4j.com/docs/http-api/current/discovery/
/// </summary>
public class ConnectivityVerifierTests
{
    private static readonly Uri BaseUri = new("https://neo4j.example.com:7474/");

    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<ConnectivityVerifier>();

    public ConnectivityVerifierTests()
    {
        var serverInfo = new QueryApiServerInfo(TestDriverContext.With(uri: BaseUri));
        _autoMocker.Use<IServerInfo>(serverInfo);
        _autoMocker.Use<IServerAgentWriter>(serverInfo);
    }

    private static ConnectivityVerifier.DiscoveryResponse ValidDiscovery(
        string? query = "http://localhost:7474/query/v2",
        string? neo4jVersion = "5.18.0")
    {
        return new ConnectivityVerifier.DiscoveryResponse { Query = query, Neo4jVersion = neo4jVersion };
    }

    private void SetupDiscoveryResponse(ConnectivityVerifier.DiscoveryResponse body)
    {
        _autoMocker.GetMock<IJsonDeserializer>()
            .Setup(x => x.DeserializeAsync<ConnectivityVerifier.DiscoveryResponse>(
                It.IsAny<Stream>(),
                JsonNamingPolicy.SnakeCaseLower,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(body);
    }

    // Sets up the minimum mock chain needed to exercise the handler without crashing:
    // Build("") → BaseUri → SendAsync(GET BaseUri) → response → DeserializeAsync → ValidDiscovery()
    private HttpResponseMessage SetupChain()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };

        _autoMocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build(string.Empty))
            .Returns(BaseUri);

        // The handler builds its own HttpRequestMessage internally, so we match on GET method
        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SetupDiscoveryResponse(ValidDiscovery());

        return response;
    }

    [Fact]
    public async Task ReturnsServerInfo_WithAddressAndVersionFromDiscovery()
    {
        // Spec: address comes from the base URI; agent version comes from the discovery body
        SetupChain();

        SetupDiscoveryResponse(ValidDiscovery(neo4jVersion: "5.22.0"));

        var subject = _autoMocker.CreateInstance<ConnectivityVerifier>();
        var serverInfo = await subject.VerifyAsync(TestContext.Current.CancellationToken);

        serverInfo.Address.Should().Be("neo4j.example.com:7474");
        serverInfo.Agent.Should().Be("5.22.0");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenQueryApiEndpointAbsentFromDiscovery()
    {
        // Server is running but does not support the Query API (e.g. Neo4j 4.x)
        SetupChain();

        SetupDiscoveryResponse(ValidDiscovery(query: null));

        var subject = _autoMocker.CreateInstance<ConnectivityVerifier>();
        var act = () => subject.VerifyAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*Query API endpoint*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenServerVersionAbsentFromDiscovery()
    {
        SetupChain();

        SetupDiscoveryResponse(ValidDiscovery(neo4jVersion: null));

        var subject = _autoMocker.CreateInstance<ConnectivityVerifier>();
        var act = () => subject.VerifyAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*server version*");
    }

    [Fact]
    public async Task ThrowsServiceUnavailableException_WhenDiscoveryThrows()
    {
        _autoMocker.GetMock<IQueryApiUrlBuilder>()
            .Setup(x => x.Build(string.Empty))
            .Returns(BaseUri);

        _autoMocker.GetMock<IQueryApiHttpTransport>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 404 GET https://neo4j.example.com:7474/ : "));

        var subject = _autoMocker.CreateInstance<ConnectivityVerifier>();
        var act = () => subject.VerifyAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*404*");
    }
}
