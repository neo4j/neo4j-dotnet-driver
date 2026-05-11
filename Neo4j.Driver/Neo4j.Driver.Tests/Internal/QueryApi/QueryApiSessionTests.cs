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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionTests
{
    private readonly Mock<IAutoCommitHandler> _autoCommitHandler = new();
    private readonly Mock<IQueryApiResultCursorBuilder> _cursorBuilder = new();
    private readonly Mock<IQueryApiTransactionFactory> _transactionFactory = new();
    private readonly SessionConfig _config = SessionConfig.Builder.Build();

    private QueryApiSession CreateSession() =>
        new(_config, _autoCommitHandler.Object, _cursorBuilder.Object, _transactionFactory.Object);

    [Fact]
    public void SessionConfig_ReturnsInjectedConfig()
    {
        CreateSession().SessionConfig.Should().BeSameAs(_config);
    }

    [Fact]
    public void LastBookmarks_InitiallyEmpty()
    {
        CreateSession().LastBookmarks.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_ReturnsCursorBuiltFromHandlerResponse()
    {
        var query = new Query("RETURN 1");
        var response = new QueryApiResponse { Fields = ["x"], Rows = [], Bookmarks = [] };
        var expectedCursor = new Mock<IResultCursor>().Object;

        _autoCommitHandler
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _cursorBuilder
            .Setup(b => b.Build(response, query))
            .Returns(expectedCursor);

        var result = await CreateSession().RunAsync(query, null!, false);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_PassesCurrentBookmarksToHandler()
    {
        var query = new Query("RETURN 1");

        _autoCommitHandler
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryApiResponse.Empty);

        _cursorBuilder
            .Setup(b => b.Build(It.IsAny<QueryApiResponse>(), query))
            .Returns(Mock.Of<IResultCursor>());

        await CreateSession().RunAsync(query, null!, false);

        _autoCommitHandler.Verify(
            h => h.AutoCommitAsync(query, Bookmarks.Empty.Values, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_UpdatesLastBookmarksFromResponse()
    {
        var query = new Query("CREATE (:Node)");
        var bookmarkValues = new[] { "neo4j:bookmark:v1:tx42" };
        var response = new QueryApiResponse { Bookmarks = bookmarkValues };

        _autoCommitHandler
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _cursorBuilder
            .Setup(b => b.Build(response, query))
            .Returns(Mock.Of<IResultCursor>());

        var session = CreateSession();
        await session.RunAsync(query, null!, false);

        session.LastBookmarks.Values.Should().BeEquivalentTo(bookmarkValues);
    }

    // --- ExecuteReadAsync ----------------------------------------------

    [Fact]
    public async Task ExecuteReadAsync_InvokesWorkAndReturnsResult()
    {
        var result = await CreateSession().ExecuteReadAsync(_ => Task.FromResult("hello"));

        result.Should().Be("hello");
    }

    [Fact]
    public async Task ExecuteReadAsync_Void_InvokesWork()
    {
        var invoked = false;
        await CreateSession().ExecuteReadAsync(_ => { invoked = true; return Task.CompletedTask; });

        invoked.Should().BeTrue();
    }

    // --- ExecuteWriteAsync ---------------------------------------------

    [Fact]
    public async Task ExecuteWriteAsync_InvokesWorkAndReturnsResult()
    {
        var result = await CreateSession().ExecuteWriteAsync(_ => Task.FromResult(42));

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWriteAsync_Void_InvokesWork()
    {
        var invoked = false;
        await CreateSession().ExecuteWriteAsync(_ => { invoked = true; return Task.CompletedTask; });

        invoked.Should().BeTrue();
    }

    // --- PipelinedExecuteReadAsync / PipelinedExecuteWriteAsync --------

    [Fact]
    public async Task PipelinedExecuteReadAsync_InvokesWorkAndReturnsResult()
    {
        var expected = new EagerResult<string>("value", Mock.Of<IResultSummary>(), []);

        var result = await CreateSession().PipelinedExecuteReadAsync(
            _ => Task.FromResult(expected),
            TransactionConfig.Default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task PipelinedExecuteWriteAsync_InvokesWorkAndReturnsResult()
    {
        var expected = new EagerResult<string>("value", Mock.Of<IResultSummary>(), []);

        var result = await CreateSession().PipelinedExecuteWriteAsync(
            _ => Task.FromResult(expected),
            TransactionConfig.Default);

        result.Should().BeSameAs(expected);
    }

    // --- BeginTransactionAsync -----------------------------------------

    [Fact]
    public async Task BeginTransactionAsync_DelegatesToFactoryWithAllParameters()
    {
        Action<TransactionConfigBuilder> configAction = _ => { };
        var expectedTx = new Mock<IAsyncTransaction>().Object;
        _transactionFactory
            .Setup(f => f.BeginTransactionAsync(
                It.IsAny<AccessMode>(),
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTx);

        var tx = await CreateSession().BeginTransactionAsync(AccessMode.Read, configAction);

        tx.Should().BeSameAs(expectedTx);
        _transactionFactory.Verify(
            f => f.BeginTransactionAsync(
                AccessMode.Read,
                configAction,
                Bookmarks.Empty.Values,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
