// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// you may not use this file except in compliance with the License.
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

public class QueryApiSessionTests
{

    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());


    [Fact]
    public async Task RunAsync_ReturnsCursorBuiltFromHandlerResponse()
    {
        var query = new Query("RETURN 1");
        var response = new QueryApiResultSet { Fields = ["x"], Rows = [], Bookmarks = [] };
        var expectedCursor = Mock.Of<IResultCursor>();
        _fixture.Freeze<Mock<IAutoCommitHandler>>()
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(b => b.Build(response, query))
            .Returns(expectedCursor);
        var sut = _fixture.Create<QueryApiSession>();

        var result = await sut.RunAsync(query, null!, false);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_PassesCurrentBookmarksToHandler()
    {
        var query = new Query("RETURN 1");
        var handler = _fixture.Freeze<Mock<IAutoCommitHandler>>();
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(b => b.Build(It.IsAny<QueryApiResultSet>(), query))
            .Returns(Mock.Of<IResultCursor>());
        var sut = _fixture.Create<QueryApiSession>();

        IReadOnlyList<string>? capturedBookmarks = null;
        handler.Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Query, IReadOnlyList<string>, CancellationToken>((_, bm, _) => capturedBookmarks = bm)
            .ReturnsAsync(QueryApiResultSet.Empty);

        await sut.RunAsync(query, null!, false);

        capturedBookmarks.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_UpdatesLastBookmarksFromResponse()
    {
        var query = new Query("CREATE (:Node)");
        var bookmarkValues = new[] { "neo4j:bookmark:v1:tx42" };
        var response = new QueryApiResultSet { Bookmarks = bookmarkValues };
        _fixture.Freeze<Mock<IAutoCommitHandler>>()
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _fixture.Freeze<Mock<IQueryApiResultCursorBuilder>>()
            .Setup(b => b.Build(response, query))
            .Returns(Mock.Of<IResultCursor>());
        var sut = _fixture.Create<QueryApiSession>();

        await sut.RunAsync(query, null!, false);

        sut.LastBookmarks.Values.Should().BeEquivalentTo(bookmarkValues);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionFromFactory()
    {
        var tx = Mock.Of<IInternalAsyncTransaction>();
        _fixture.Freeze<Mock<IQueryApiTransactionFactory>>()
            .Setup(f => f.BeginTransactionAsync(
                AccessMode.Read,
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);
        var sut = _fixture.Create<QueryApiSession>();

        var result = await sut.BeginTransactionAsync(AccessMode.Read);

        result.Should().BeSameAs(tx);
    }

    [Fact]
    public async Task ExecuteWriteAsync_WhenCommitFails_ThrowsOriginalExceptionEvenIfRollbackAlsoFails()
    {
        var commitError = new ServiceUnavailableException("HTTP 500");
        var tx = new Mock<IInternalAsyncTransaction>();
        tx.SetupGet(t => t.IsOpen).Returns(true);
        tx.Setup(t => t.CommitAsync()).ThrowsAsync(commitError);
        tx.Setup(t => t.RollbackAsync()).ThrowsAsync(new ServiceUnavailableException("rollback also failed"));
        _fixture.Freeze<Mock<IQueryApiTransactionFactory>>()
            .Setup(f => f.BeginTransactionAsync(
                It.IsAny<AccessMode>(),
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        var sut = _fixture.Create<QueryApiSession>();

        var act = () => sut.ExecuteWriteAsync<int>(_ => Task.FromResult(0));

        await act.Should().ThrowAsync<ServiceUnavailableException>().WithMessage("HTTP 500");
    }

    [Fact]
    public async Task ExecuteReadAsync_PassesTransactionToWorkAndReturnsResult()
    {
        var tx = new Mock<IInternalAsyncTransaction>();
        tx.Setup(t => t.CommitAsync()).Returns(Task.CompletedTask);
        _fixture.Freeze<Mock<IQueryApiTransactionFactory>>()
            .Setup(f => f.BeginTransactionAsync(
                AccessMode.Read,
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        var sut = _fixture.Create<QueryApiSession>();

        IAsyncQueryRunner? capturedRunner = null;
        var result = await sut.ExecuteReadAsync<int>(runner =>
        {
            capturedRunner = runner;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        capturedRunner.Should().BeSameAs(tx.Object);
    }
}
