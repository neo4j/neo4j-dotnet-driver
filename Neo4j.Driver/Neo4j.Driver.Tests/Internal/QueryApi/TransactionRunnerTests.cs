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
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Running a query within an explicit transaction requires the transaction ID in the URL and, for Aura instances,
/// the <c>neo4j-cluster-affinity</c> header forwarded from BEGIN. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-query-in-transaction
/// </summary>
public class TransactionRunnerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private void SetupChain(QueryApiResultBody? body = null)
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        body ??= new QueryApiResultBody();
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync($"query/v2/tx/{txContext.TxId}", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<QueryApiResultBody>(body, new HttpResponseMessage().Headers));
    }

    [Fact]
    public async Task RunAsync_ReturnsCursor_BuiltFromResultSet()
    {
        // Spec: successful response contains data.fields, data.values; these are built into an IResultCursor
        SetupChain(new QueryApiResultBody
        {
            Data = new QueryApiDataBody { Fields = ["x"] }
        });

        var expectedCursor = new Mock<IResultCursor>().Object;
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(x => x.Build(It.IsAny<QueryApiResultSet>(), It.IsAny<Query>()))
            .Returns(expectedCursor);

        var subject = _fixture.Create<TransactionRunner>();
        var cursor = await subject.RunAsync(
            new Query("RETURN 42 AS x"),
            TestContext.Current.CancellationToken);

        cursor.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenExecuteAsyncThrows()
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync($"query/v2/tx/{txContext.TxId}", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _fixture.Create<TransactionRunner>();
        var act = () => subject.RunAsync(new Query("RETURN 1"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }
}
