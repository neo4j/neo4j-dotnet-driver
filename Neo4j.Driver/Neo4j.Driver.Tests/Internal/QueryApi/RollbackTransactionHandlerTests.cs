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
/// Rolling back a transaction discards all statements run within it. A DELETE request is used — the only Query
/// API operation that is not a POST. Spec: https://neo4j.com/docs/query-api/current/#query-api-rollback-transaction
/// </summary>
public class RollbackTransactionHandlerTests
{
    private static readonly IAuthToken AnyAuth = AuthTokens.Basic("user", "pass");
    private static readonly QueryApiTransactionContext TxWithAffinity = new("tx-77", "shard-5");
    private static readonly QueryApiTransactionContext TxWithoutAffinity = new("tx-77", null);

    private static AutoMocker CreateMocker(
        FakeQueryApiHttpClient httpClient,
        string database = "neo4j",
        QueryApiTransactionContext? txContext = null)
    {
        txContext ??= TxWithoutAffinity;
        var mocker = new AutoMocker();
        mocker.Use<IQueryApiHttpClient>(httpClient);
        mocker.Use<QueryApiTransactionContext>(txContext);
        mocker.Use<IQueryApiRequestBuilder>(new QueryApiRequestBuilder(UrlBuilder, new SessionContext(database), new QueryApiAuthApplicator(), new QueryApiClusterAffinityApplicator(), txContext));
        return mocker;
    }

    [Fact]
    public async Task SendsDelete_ToTransactionEndpoint_WithTransactionId()
    {
        // DELETE /db/{database}/query/v2/tx/{txId}
        var httpClient = new FakeQueryApiHttpClient(Accepted());
        var handler = CreateMocker(httpClient, database: "movies", txContext: TxWithoutAffinity).CreateInstance<RollbackTransactionHandler>();

        await handler.RollbackTransactionAsync(AnyAuth);

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        httpClient.LastRequest.RequestUri!.PathAndQuery.Should().Be("/db/movies/query/v2/tx/tx-77");
    }

    [Fact]
    public async Task ForwardsClusterAffinityHeader_OnRequest()
    {
        // Spec: cluster affinity must be forwarded on ROLLBACK as well
        var httpClient = new FakeQueryApiHttpClient(Accepted());
        var handler = CreateMocker(httpClient, txContext: TxWithAffinity).CreateInstance<RollbackTransactionHandler>();

        await handler.RollbackTransactionAsync(AnyAuth);

        httpClient.LastRequest!.Headers.GetValues("neo4j-cluster-affinity").Should().Equal("shard-5");
    }

    [Fact]
    public async Task CallsErrorChecker_OnResponse()
    {
        var mocker = CreateMocker(new FakeQueryApiHttpClient(Accepted()));

        await mocker.CreateInstance<RollbackTransactionHandler>()
            .RollbackTransactionAsync(AnyAuth);

        mocker.GetMock<IQueryApiErrorChecker>()
            .Verify(x => x.EnsureSuccessAsync(It.IsAny<HttpResponseMessage>(), default), Times.Once);
    }
}
