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
/// Running a query within an explicit transaction requires the transaction ID in the URL and, for Aura instances,
/// the <c>neo4j-cluster-affinity</c> header forwarded from BEGIN. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-query-in-transaction
/// </summary>
public class RunInTransactionHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");
    private static readonly QueryApiTransactionContext TxWithAffinity = new("tx-99", "shard-7");
    private static readonly QueryApiTransactionContext TxWithoutAffinity = new("tx-99", null);

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
    public async Task SendsPost_ToTransactionRunEndpoint_WithTransactionId()
    {
        // POST /db/{database}/query/v2/tx/{txId}
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient, database: "movies").CreateInstance<RunInTransactionHandler>();

        await handler.RunInTransactionAsync(TxWithoutAffinity, new Query("RETURN 1"), AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/movies/query/v2/tx/tx-99");
    }

    [Fact]
    public async Task RequestBody_ContainsStatement()
    {
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient).CreateInstance<RunInTransactionHandler>();

        await handler.RunInTransactionAsync(TxWithoutAffinity, new Query("MATCH (n) RETURN n"), AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("statement").GetString().Should().Be("MATCH (n) RETURN n");
    }

    [Fact]
    public async Task RequestBody_IncludesParameters_WhenQueryHasParameters()
    {
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var query = new Query("MATCH (n {id: $id}) RETURN n", new Dictionary<string, object> { ["id"] = 7 });
        var handler = CreateMocker(httpClient).CreateInstance<RunInTransactionHandler>();

        await handler.RunInTransactionAsync(TxWithoutAffinity, query, AnyAuth);

        var body = JsonDocument.Parse(httpClient.LastRequestBody!).RootElement;
        body.GetProperty("parameters").GetProperty("id").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task ForwardsClusterAffinityHeader_OnRequest()
    {
        // Spec: the cluster affinity received on BEGIN must be forwarded on all subsequent requests
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(EmptyDataResponse()));
        var handler = CreateMocker(httpClient).CreateInstance<RunInTransactionHandler>();

        await handler.RunInTransactionAsync(TxWithAffinity, new Query("RETURN 1"), AnyAuth);

        httpClient.LastRequest!.Headers.GetValues("neo4j-cluster-affinity").Should().Equal("shard-7");
    }

    [Fact]
    public async Task Response_ReturnsFieldsRowsAndBookmarks()
    {
        var httpClient = new FakeQueryApiHttpClient(
            AcceptedWith(
                new
                {
                    data = new
                    {
                        fields = new[] { "x" },
                        values = new[] { new object[] { 42 } }
                    },
                    bookmarks = new[] { "neo4j:bookmark:v1:tx200" }
                }));
        var handler = CreateMocker(httpClient).CreateInstance<RunInTransactionHandler>();

        var result = await handler.RunInTransactionAsync(TxWithoutAffinity, new Query("RETURN 42 AS x"), AnyAuth);

        result.Fields.Should().Equal("x");
        result.Rows.Should().HaveCount(1);
        result.Bookmarks.Should().Equal("neo4j:bookmark:v1:tx200");
    }

    private static object EmptyDataResponse()
    {
        return new
        {
            data = new { fields = Array.Empty<string>(), values = Array.Empty<object[]>() }
        };
    }
}
