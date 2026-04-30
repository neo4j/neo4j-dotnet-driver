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
/// Committing a transaction finalises all statements run within it and returns updated bookmarks. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-commit-transaction
/// </summary>
public class CommitTransactionHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");
    private static readonly QueryApiTransactionContext TxWithAffinity = new("tx-55", "shard-3");
    private static readonly QueryApiTransactionContext TxWithoutAffinity = new("tx-55", null);

    private static AutoMocker CreateMocker(FakeQueryApiHttpClient httpClient, string database = "neo4j")
    {
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<IQueryApiRequestBuilder>(new QueryApiRequestBuilder(UrlBuilder, new SessionContext(database), new QueryApiAuthApplicator(), new QueryApiClusterAffinityApplicator()));
        mocker.Use<IJsonDeserializer>(new QueryApiJsonSerializer());
        return mocker;
    }

    [Fact]
    public async Task SendsPost_ToCommitEndpoint_WithTransactionId()
    {
        // POST /db/{database}/query/v2/tx/{txId}/commit
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(new {}));
        var handler = CreateMocker(httpClient, database: "movies").CreateInstance<CommitTransactionHandler>();

        await handler.CommitTransactionAsync(TxWithoutAffinity, AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Post);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/movies/query/v2/tx/tx-55/commit");
    }

    [Fact]
    public async Task ReturnsBookmarks_FromCommitResponse()
    {
        // Spec: the commit response contains updated bookmarks for causal consistency
        var httpClient = new FakeQueryApiHttpClient(
            AcceptedWith(
                new
                {
                    bookmarks = new[] { "neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301" }
                }));
        var handler = CreateMocker(httpClient).CreateInstance<CommitTransactionHandler>();

        var bookmarks = await handler.CommitTransactionAsync(TxWithoutAffinity, AnyAuth);

        bookmarks.Should().Equal("neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301");
    }

    [Fact]
    public async Task ReturnsEmptyBookmarks_WhenResponseBodyIsEmpty()
    {
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(new {}));
        var handler = CreateMocker(httpClient).CreateInstance<CommitTransactionHandler>();

        var bookmarks = await handler.CommitTransactionAsync(TxWithoutAffinity, AnyAuth);

        bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task ForwardsClusterAffinityHeader_OnRequest()
    {
        // Spec: cluster affinity must be forwarded on COMMIT as well
        var httpClient = new FakeQueryApiHttpClient(AcceptedWith(new {}));
        var handler = CreateMocker(httpClient).CreateInstance<CommitTransactionHandler>();

        await handler.CommitTransactionAsync(TxWithAffinity, AnyAuth);

        httpClient.LastRequest!.Headers.GetValues("neo4j-cluster-affinity").Should().Equal("shard-3");
    }
}
