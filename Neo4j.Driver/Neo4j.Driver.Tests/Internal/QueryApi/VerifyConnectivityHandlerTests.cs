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
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiTestHelpers;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Connectivity verification issues a simple <c>RETURN 1</c> against the <c>system</c> database — a minimal
/// round-trip that confirms the server is reachable and authentication is valid. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-autocommit-query
/// </summary>
public class VerifyConnectivityHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");

    private static AutoMocker CreateMocker(FakeQueryApiHttpClient httpClient)
    {
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<IQueryApiUrlBuilder>(UrlBuilder);
        mocker.Use<IJsonSerializer>(new QueryApiJsonSerializer());
        return mocker;
    }

    [Fact]
    public async Task SendsPost_ToSystemDatabase_WithReturnOneStatement()
    {
        // Connectivity check always targets the system database with a trivial query
        var httpClient = new FakeQueryApiHttpClient(Accepted());
        var handler = CreateMocker(httpClient).CreateInstance<VerifyConnectivityHandler>();

        await handler.VerifyConnectivityAsync(AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/system/query/v2");

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("statement").GetString().Should().Be("RETURN 1");
    }

    [Fact]
    public async Task ReturnsServerInfo_WithHostAndPort_FromBaseUri()
    {
        var handler = CreateMocker(new FakeQueryApiHttpClient(Accepted())).CreateInstance<VerifyConnectivityHandler>();

        var serverInfo = await handler.VerifyConnectivityAsync(AnyAuth);

        serverInfo.Address.Should().Be("localhost:7474");
    }

    [Fact]
    public async Task ReturnsServerInfo_WithAgent_FromServerResponseHeader()
    {
        var response = Accepted();
        response.Headers.Server.Add(new ProductInfoHeaderValue("Neo4j", "5.18.0"));
        var handler = CreateMocker(new FakeQueryApiHttpClient(response)).CreateInstance<VerifyConnectivityHandler>();

        var serverInfo = await handler.VerifyConnectivityAsync(AnyAuth);

        serverInfo.Agent.Should().Contain("Neo4j/5.18.0");
    }

    [Fact]
    public async Task ReturnsServerInfo_WithEmptyAgent_WhenNoServerHeader()
    {
        var handler = CreateMocker(new FakeQueryApiHttpClient(Accepted())).CreateInstance<VerifyConnectivityHandler>();

        var serverInfo = await handler.VerifyConnectivityAsync(AnyAuth);

        serverInfo.Agent.Should().BeEmpty();
    }

    [Fact]
    public async Task CallsAuthApplicator_WithProvidedToken()
    {
        var token = AuthTokens.Basic("neo4j", "password");
        var mocker = CreateMocker(new FakeQueryApiHttpClient(Accepted()));

        await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync(token);

        mocker.GetMock<IAuthApplicator>().Verify(x => x.Apply(It.IsAny<HttpRequestMessage>(), token), Times.Once);
    }

    [Fact]
    public async Task CallsErrorChecker_OnResponse()
    {
        var mocker = CreateMocker(new FakeQueryApiHttpClient(Accepted()));

        await mocker.CreateInstance<VerifyConnectivityHandler>().VerifyConnectivityAsync(AnyAuth);

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), default), Times.Once);
    }
}
