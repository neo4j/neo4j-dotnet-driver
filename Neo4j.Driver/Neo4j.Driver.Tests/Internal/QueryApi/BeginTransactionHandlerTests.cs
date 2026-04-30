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
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;
using static Neo4j.Driver.Tests.Internal.QueryApi.QueryApiTestHelpers;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Beginning a transaction reserves a server-side transaction and returns its ID, plus the optional
/// <c>neo4j-cluster-affinity</c> header that must be forwarded on all subsequent requests. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-begin-transaction
/// </summary>
public class BeginTransactionHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");

    private static AutoMocker CreateMocker(FakeQueryApiHttpClient httpClient, string database = "neo4j")
    {
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<IQueryApiRequestBuilder>(new QueryApiRequestBuilder(UrlBuilder, new SessionContext(database), new QueryApiAuthApplicator(), new QueryApiClusterAffinityApplicator()));
        var json = new QueryApiJsonSerializer();
        mocker.Use<IJsonDeserializer>(json);
        mocker.Use<IJsonSerializer>(json);
        return mocker;
    }

    [Fact]
    public async Task SendsPost_ToBeginTransactionEndpoint()
    {
        // POST /db/{database}/query/v2/tx
        var httpClient = new FakeQueryApiHttpClient(BeginResponseWith("tx-1"));
        var handler = CreateMocker(httpClient, database: "movies").CreateInstance<BeginTransactionHandler>();

        await handler.BeginTransactionAsync([], AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/movies/query/v2/tx");
    }

    [Fact]
    public async Task ReturnsTransactionId_FromResponseBody()
    {
        // Spec: response body contains transaction.id — the handle for subsequent requests
        var mocker = CreateMocker(new FakeQueryApiHttpClient(BeginResponseWith("tx-abc-123")));

        var context = await mocker.CreateInstance<BeginTransactionHandler>()
            .BeginTransactionAsync([], AnyAuth);

        context.TxId.Should().Be("tx-abc-123");
    }

    [Fact]
    public async Task ReturnsClusterAffinity_WhenResponseCarriesAffinityHeader()
    {
        // Spec: Aura instances return neo4j-cluster-affinity on BEGIN — it must be echoed back
        var mocker = CreateMocker(new FakeQueryApiHttpClient(BeginResponseWith("tx-1")));
        mocker.GetMock<IClusterAffinityApplicator>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseMessage>()))
            .Returns("shard-99");

        var context = await mocker.CreateInstance<BeginTransactionHandler>()
            .BeginTransactionAsync([], AnyAuth);

        context.ClusterAffinity.Should().Be("shard-99");
    }

    [Fact]
    public async Task ReturnsNullClusterAffinity_WhenResponseHasNoAffinityHeader()
    {
        var mocker = CreateMocker(new FakeQueryApiHttpClient(BeginResponseWith("tx-1")));
        mocker.GetMock<IClusterAffinityApplicator>()
            .Setup(x => x.Extract(It.IsAny<HttpResponseMessage>()))
            .Returns((string?)null);

        var context = await mocker.CreateInstance<BeginTransactionHandler>()
            .BeginTransactionAsync([], AnyAuth);

        context.ClusterAffinity.Should().BeNull();
    }

    [Fact]
    public async Task RequestBody_IncludesBookmarks_WhenProvided()
    {
        // Spec: bookmarks enable causal consistency at transaction start
        var httpClient = new FakeQueryApiHttpClient(BeginResponseWith("tx-1"));
        var bookmarks = new List<string> { "neo4j:bookmark:v1:tx50" };
        var handler = CreateMocker(httpClient).CreateInstance<BeginTransactionHandler>();

        await handler.BeginTransactionAsync(bookmarks, AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("bookmarks")[0].GetString().Should().Be("neo4j:bookmark:v1:tx50");
    }

    [Fact]
    public async Task RequestBody_OmitsBookmarks_WhenListIsEmpty()
    {
        var httpClient = new FakeQueryApiHttpClient(BeginResponseWith("tx-1"));
        var handler = CreateMocker(httpClient).CreateInstance<BeginTransactionHandler>();

        await handler.BeginTransactionAsync([], AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.TryGetProperty("bookmarks", out var _).Should().BeFalse();
    }

    [Fact]
    public async Task Throws_WhenResponseDoesNotContainTransactionId()
    {
        // A missing transaction ID means something went wrong server-side
        var mocker = CreateMocker(new FakeQueryApiHttpClient(AcceptedWith(new { transaction = new {} })));

        var act = () => mocker.CreateInstance<BeginTransactionHandler>()
            .BeginTransactionAsync([], AnyAuth);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction ID*");
    }

    private static HttpResponseMessage BeginResponseWith(string txId)
    {
        return AcceptedWith(new { transaction = new { id = txId } });
    }
}
