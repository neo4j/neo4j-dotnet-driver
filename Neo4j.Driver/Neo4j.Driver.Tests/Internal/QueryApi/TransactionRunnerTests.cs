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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Running a query within an explicit transaction requires the transaction ID in the URL and, for Aura instances,
/// the <c>neo4j-cluster-affinity</c> header forwarded from BEGIN. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-run-query-in-transaction
/// </summary>
public class TransactionRunnerTests
{
    private const string TxId = "tx-1";

    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<TransactionRunner>();

    public TransactionRunnerTests()
    {
        _autoMocker.GetMock<IQueryApiTransactionContextTracker>()
            .SetupGet(x => x.Context)
            .Returns(new QueryApiTransactionContext(TxId, null));
    }

    private void SetupChain(QueryApiResultBody? body = null)
    {
        body ??= new QueryApiResultBody();
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync($"query/v2/tx/{TxId}", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _autoMocker.GetMock<IQueryApiClient>()
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
        _autoMocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(x => x.Build(It.IsAny<QueryApiResultSet>(), It.IsAny<Query>()))
            .Returns(expectedCursor);

        var subject = _autoMocker.CreateInstance<TransactionRunner>();
        var cursor = await subject.RunAsync(
            new Query("RETURN 42 AS x"),
            TestContext.Current.CancellationToken);

        cursor.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_Throws_WhenExecuteAsyncThrows()
    {
        var request = new HttpRequestMessage();

        _autoMocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync($"query/v2/tx/{TxId}", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _autoMocker.GetMock<IQueryApiClient>()
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _autoMocker.CreateInstance<TransactionRunner>();
        var act = () => subject.RunAsync(new Query("RETURN 1"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task RunAsync_MarksTransactionFailed_WhenServerReturnsClientError()
    {
        var request = new HttpRequestMessage();
        var txContextTracker = _autoMocker.GetMock<IQueryApiTransactionContextTracker>();

        _autoMocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync($"query/v2/tx/{TxId}", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _autoMocker.GetMock<IQueryApiClient>()
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientException("Neo.ClientError.Statement.SyntaxError", "Invalid input"));

        var subject = _autoMocker.CreateInstance<TransactionRunner>();
        var act = () => subject.RunAsync(new Query("Invalid Cypher"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClientException>();

        txContextTracker.Verify(x => x.MarkFailed(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_DoesNotMarkTransactionFailed_WhenServiceUnavailable()
    {
        var request = new HttpRequestMessage();
        var txContextTracker = _autoMocker.GetMock<IQueryApiTransactionContextTracker>();

        _autoMocker.GetMock<IQueryApiRequestBuilder>()
            .Setup(x => x.PostAsync($"query/v2/tx/{TxId}", It.IsAny<IQueryApiRequestBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _autoMocker.GetMock<IQueryApiClient>()
            .Setup(x => x.ExecuteAsync<QueryApiResultBody>(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _autoMocker.CreateInstance<TransactionRunner>();
        var act = () => subject.RunAsync(new Query("RETURN 1"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();

        txContextTracker.Verify(x => x.MarkFailed(), Times.Never);
    }
}
