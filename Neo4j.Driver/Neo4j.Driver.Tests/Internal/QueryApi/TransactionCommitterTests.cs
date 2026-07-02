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
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>
/// Committing a transaction finalises all statements run within it and returns updated bookmarks. Spec:
/// https://neo4j.com/docs/query-api/current/#query-api-commit-transaction
/// </summary>
public class TransactionCommitterTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    private void SetupChain(TransactionCommitter.ResponseBody? body = null)
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        body ??= new TransactionCommitter.ResponseBody();
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(
                $"query/v2/tx/{txContext.TxId}/commit",
                It.IsAny<IQueryApiRequestBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<TransactionCommitter.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryApiResult<TransactionCommitter.ResponseBody>(body, new HttpResponseMessage().Headers));
    }

    [Fact]
    public async Task CommitAsync_UpdatesBookmarkTracker_WithServerBookmarks()
    {
        // Spec: commit response bookmarks must be applied to the session's tracker for causal chaining
        SetupChain(new TransactionCommitter.ResponseBody
        {
            Bookmarks = ["neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301"]
        });

        var tracker = new BookmarkTracker(SessionConfig.Builder.Build());
        _fixture.Inject<IBookmarkTracker>(tracker);

        var subject = _fixture.Create<TransactionCommitter>();
        await subject.CommitAsync(TestContext.Current.CancellationToken);

        tracker.CurrentBookmarks.Values.Should().Equal("neo4j:bookmark:v1:tx300", "neo4j:bookmark:v1:tx301");
    }

    [Fact]
    public async Task CommitAsync_UpdatesBookmarkTracker_WithEmptyArray_WhenBodyHasNoBookmarks()
    {
        SetupChain();

        var tracker = new BookmarkTracker(SessionConfig.Builder.Build());
        tracker.UpdateBookmarks(["pre-existing"]);
        _fixture.Inject<IBookmarkTracker>(tracker);

        var subject = _fixture.Create<TransactionCommitter>();
        await subject.CommitAsync(TestContext.Current.CancellationToken);

        tracker.CurrentBookmarks.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_Throws_WhenExecuteAsyncThrows()
    {
        var txContext = _fixture.Freeze<QueryApiTransactionContext>();
        var request = new HttpRequestMessage();

        _fixture.Freeze<Mock<IQueryApiRequestBuilder>>()
            .Setup(x => x.PostAsync(
                $"query/v2/tx/{txContext.TxId}/commit",
                It.IsAny<IQueryApiRequestBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _fixture.Freeze<Mock<IQueryApiClient>>()
            .Setup(x => x.ExecuteAsync<TransactionCommitter.ResponseBody>(
                request,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("HTTP 503"));

        var subject = _fixture.Create<TransactionCommitter>();
        var act = () => subject.CommitAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }
}
