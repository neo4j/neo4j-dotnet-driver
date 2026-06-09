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

using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiTransactionTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    [Fact]
    public void TransactionConfig_ReturnsDefault()
    {
        var subject = _fixture.Create<QueryApiTransaction>();

        subject.TransactionConfig.Should().BeSameAs(TransactionConfig.Default);
    }

    [Fact]
    public async Task RunAsync_ReturnsCursorBuiltFromHandlerResponse()
    {
        var query = new Query("RETURN 1");
        var response = new QueryApiResultSet { Fields = ["x"], Rows = [], Bookmarks = [] };
        
        _fixture.Freeze<Mock<IRunInTransactionHandler>>()
            .Setup(h => h.RunInTransactionAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var expectedCursor = Mock.Of<IResultCursor>();
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(b => b.Build(response, query))
            .Returns(expectedCursor);

        var subject = _fixture.Create<QueryApiTransaction>();

        var result = await subject.RunAsync(query);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_String_DelegatesToQueryOverload()
    {
        var response = QueryApiResultSet.Empty;

        _fixture.Freeze<Mock<IRunInTransactionHandler>>()
            .Setup(h => h.RunInTransactionAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var expectedCursor = Mock.Of<IResultCursor>();
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(b => b.Build(response, It.Is<Query>(q => q.Text == "RETURN 1")))
            .Returns(expectedCursor);

        var subject = _fixture.Create<QueryApiTransaction>();

        var result = await subject.RunAsync("RETURN 1");

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterCommit()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.CommitAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RollbackAsync_ThrowsAfterCommit()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.RollbackAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterCommit()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.RunAsync(new Query("RETURN 1"));
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterRollback()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.RollbackAsync();

        var act = () => subject.RunAsync(new Query("RETURN 1"));
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterRollback()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.RollbackAsync();

        var act = () => subject.CommitAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public void IsOpen_TrueInitially()
    {
        _fixture.Create<QueryApiTransaction>().IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task IsOpen_FalseAfterCommit()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.CommitAsync();

        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task IsOpen_FalseAfterRollback()
    {
        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.RollbackAsync();

        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CommitAsync_ForwardsBookmarksFromHandlerToTracker()
    {
        var bookmarks = new[] { "neo4j:bookmark:v1:tx42", "another one" };
        
        _fixture.Freeze<Mock<ICommitTransactionHandler>>()
            .Setup(h => h.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookmarks);

        var tracker = new BookmarkTracker(SessionConfig.Builder.Build());
        _fixture.Inject<IBookmarkTracker>(tracker);

        var subject = _fixture.Create<QueryApiTransaction>();
        
        await subject.CommitAsync();

        tracker.CurrentBookmarks.Values.Should().BeEquivalentTo(bookmarks);
    }

    [Fact]
    public async Task DisposeAsync_WhenOpen_RollsBack()
    {
        var rollbackCalled = false;

        _fixture.Freeze<Mock<IRollbackTransactionHandler>>()
            .Setup(h => h.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCalled = true)
            .Returns(Task.CompletedTask);

        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.DisposeAsync();

        rollbackCalled.Should().BeTrue();
        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenCommitted_DoesNotRollBack()
    {
        var rollbackCalled = false;
        
        _fixture.Freeze<Mock<IRollbackTransactionHandler>>()
            .Setup(h => h.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCalled = true)
            .Returns(Task.CompletedTask);

        var subject = _fixture.Create<QueryApiTransaction>();
        await subject.CommitAsync();
        await subject.DisposeAsync();

        rollbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenAlreadyRolledBack_DoesNotRollBackAgain()
    {
        var rollbackCount = 0;

        _fixture.Freeze<Mock<IRollbackTransactionHandler>>()
            .Setup(h => h.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCount++)
            .Returns(Task.CompletedTask);

        var subject = _fixture.Create<QueryApiTransaction>();

        await subject.RollbackAsync();
        await subject.DisposeAsync();

        rollbackCount.Should().Be(1);
    }
}
