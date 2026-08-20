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
/// Rolling back a transaction discards all statements run within it. A DELETE request is used — the only Query
/// API operation that is not a POST. Spec: https://neo4j.com/docs/query-api/current/#query-api-rollback-transaction
/// </summary>
public class TransactionRollbackerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    // Freezes the minimum mock chain needed to exercise the handler without crashing:
    // DeleteAsync("query/v2/tx/{txId}") → request → SendAsync(request) → response
    private HttpResponseMessage SetupChain()
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiTransactionContextTracker>>()
            .Setup(x => x.IsFailed)
            .Returns(false);

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.DeleteAsync($"query/v2/tx/{txContext.TxId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var response = new HttpResponseMessage();
        _fixture.Freeze<Mock<IQueryApiHttpTransport>>()
            .Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return response;
    }

    [Fact]
    public async Task Throws_WhenHttpClientThrows()
    {
        SetupChain();
        _fixture.Freeze<Mock<IQueryApiHttpTransport>>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _fixture.Create<TransactionRollbacker>();
        var act = () => subject.RollbackAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }

    [Fact]
    public async Task DoesNotSendRequest_WhenTransactionAlreadyFailed()
    {
        var requestBuilder = _fixture.Freeze<Mock<IQueryApiRequestBuilder>>();
        _fixture.Freeze<Mock<IQueryApiTransactionContextTracker>>()
            .Setup(x => x.IsFailed)
            .Returns(true);

        var subject = _fixture.Create<TransactionRollbacker>();
        var act = () => subject.RollbackAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();

        requestBuilder.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
