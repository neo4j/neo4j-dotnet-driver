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

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Neo4j.Driver.Internal.QueryApi.Types;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Committing a transaction finalises all statements run within it and returns updated bookmarks. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-commit-transaction
/// </summary>
public class CommitTransactionHandlerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    // Freezes the minimum mock chain needed to exercise the handler without crashing:
    // PostAsync("query/v2/tx/{txId}/commit") → request → SendAsync(request) → response
    private HttpResponseMessage SetupChain()
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        var request = new HttpRequestMessage();
        var response = new HttpResponseMessage { Content = new ByteArrayContent([]) };

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(
                $"query/v2/tx/{txContext.TxId}/commit",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return response;
    }

    [Fact]
    public async Task ReturnsBookmarks_FromDeserializedBody()
    {
        // Spec: the commit response contains updated bookmarks for causal consistency
        SetupChain();

        string[] expectedBookmarks = ["neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301"];

        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<CommitTransactionHandler.ResponseBody>(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CommitTransactionHandler.ResponseBody
                {
                    Bookmarks = expectedBookmarks
                });

        var subject = _fixture.Create<CommitTransactionHandler>();
        var bookmarks = await subject.CommitTransactionAsync(TestContext.Current.CancellationToken);

        bookmarks.Should().Equal("neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301");
    }

    [Fact]
    public async Task ReturnsEmptyBookmarks_WhenBodyHasNoBookmarks()
    {
        SetupChain();

        _fixture.Freeze<Mock<IJsonDeserializer>>()
            .Setup(x => x.DeserializeAsync<CommitTransactionHandler.ResponseBody>(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommitTransactionHandler.ResponseBody());

        var subject = _fixture.Create<CommitTransactionHandler>();
        var bookmarks = await subject.CommitTransactionAsync(TestContext.Current.CancellationToken);

        bookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_WhenHttpClientThrows()
    {
        SetupChain();
        _fixture.Freeze<Mock<IQueryApiHttpClient>>()
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _fixture.Create<CommitTransactionHandler>();
        var act = () => subject.CommitTransactionAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }
}
