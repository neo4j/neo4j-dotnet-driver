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
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiTransactionTests
{
    private readonly AutoMocker _mocker = new();

    private QueryApiTransaction CreateTransaction() => _mocker.CreateInstance<QueryApiTransaction>();

    [Fact]
    public void TransactionConfig_ReturnsDefault()
    {
        CreateTransaction().TransactionConfig.Should().BeSameAs(TransactionConfig.Default);
    }

    [Fact]
    public async Task RunAsync_ReturnsCursorBuiltFromHandlerResponse()
    {
        var query = new Query("RETURN 1");
        var response = new QueryApiResponse { Fields = ["x"], Rows = [], Bookmarks = [] };
        var expectedCursor = new Mock<IResultCursor>().Object;

        _mocker.GetMock<IRunInTransactionHandler>()
            .Setup(h => h.RunInTransactionAsync(query, default))
            .ReturnsAsync(response);

        _mocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(b => b.Build(response, query))
            .Returns(expectedCursor);

        var result = await CreateTransaction().RunAsync(query);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_String_DelegatesToQueryOverload()
    {
        var response = QueryApiResponse.Empty;
        var expectedCursor = new Mock<IResultCursor>().Object;

        _mocker.GetMock<IRunInTransactionHandler>()
            .Setup(h => h.RunInTransactionAsync(It.IsAny<Query>(), default))
            .ReturnsAsync(response);

        _mocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(b => b.Build(response, It.Is<Query>(q => q.Text == "RETURN 1")))
            .Returns(expectedCursor);

        var result = await CreateTransaction().RunAsync("RETURN 1");

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterCommit()
    {
        var tx = CreateTransaction();
        await tx.CommitAsync();

        await tx.Invoking(t => t.CommitAsync())
            .Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RollbackAsync_ThrowsAfterCommit()
    {
        var tx = CreateTransaction();
        await tx.CommitAsync();

        await tx.Invoking(t => t.RollbackAsync())
            .Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterCommit()
    {
        var tx = CreateTransaction();
        await tx.CommitAsync();

        await tx.Invoking(t => t.RunAsync(new Query("RETURN 1")))
            .Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterRollback()
    {
        var tx = CreateTransaction();
        await tx.RollbackAsync();

        await tx.Invoking(t => t.RunAsync(new Query("RETURN 1")))
            .Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterRollback()
    {
        var tx = CreateTransaction();
        await tx.RollbackAsync();

        await tx.Invoking(t => t.CommitAsync())
            .Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public void IsOpen_TrueInitially()
    {
        CreateTransaction().IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task IsOpen_FalseAfterCommit()
    {
        var tx = CreateTransaction();
        await tx.CommitAsync();

        tx.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task IsOpen_FalseAfterRollback()
    {
        var tx = CreateTransaction();
        await tx.RollbackAsync();

        tx.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CommitAsync_ReturnsBookmarksFromHandler()
    {
        var bookmarks = new[] { "neo4j:bookmark:v1:tx42" };
        _mocker.GetMock<ICommitTransactionHandler>()
            .Setup(h => h.CommitTransactionAsync(default))
            .ReturnsAsync(bookmarks);

        var result = await ((IInternalAsyncTransaction)CreateTransaction()).CommitAsync();

        result.Should().BeEquivalentTo(bookmarks);
    }

    [Fact]
    public async Task DisposeAsync_WhenOpen_RollsBack()
    {
        var tx = CreateTransaction();
        await tx.DisposeAsync();

        _mocker.GetMock<IRollbackTransactionHandler>()
            .Verify(h => h.RollbackTransactionAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenCommitted_DoesNotRollBack()
    {
        var tx = CreateTransaction();
        await tx.CommitAsync();
        await tx.DisposeAsync();

        _mocker.GetMock<IRollbackTransactionHandler>()
            .Verify(h => h.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_WhenAlreadyRolledBack_DoesNotRollBackAgain()
    {
        var tx = CreateTransaction();
        await tx.RollbackAsync();
        await tx.DisposeAsync();

        _mocker.GetMock<IRollbackTransactionHandler>()
            .Verify(h => h.RollbackTransactionAsync(default), Times.Once);
    }
}
