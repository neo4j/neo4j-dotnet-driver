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
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionTests
{
    private readonly AutoMocker _mocker = new();
    private readonly SessionConfig _config = SessionConfig.Builder.Build();

    public QueryApiSessionTests()
    {
        _mocker.Use(_config);
    }

    private QueryApiSession CreateSession() => _mocker.CreateInstance<QueryApiSession>();

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
        var response = new QueryApiResultSet { Fields = ["x"], Rows = [], Bookmarks = [] };
        var expectedCursor = new Mock<IResultCursor>().Object;

        _mocker.GetMock<IAutoCommitHandler>()
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(b => b.Build(response, query))
            .Returns(expectedCursor);

        var result = await CreateSession().RunAsync(query, null!, false);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_PassesCurrentBookmarksToHandler()
    {
        var query = new Query("RETURN 1");

        _mocker.GetMock<IAutoCommitHandler>()
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueryApiResultSet.Empty);

        _mocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(b => b.Build(It.IsAny<QueryApiResultSet>(), query))
            .Returns(Mock.Of<IResultCursor>());

        await CreateSession().RunAsync(query, null!, false);

        _mocker.GetMock<IAutoCommitHandler>().Verify(
            h => h.AutoCommitAsync(query, Bookmarks.Empty.Values, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_UpdatesLastBookmarksFromResponse()
    {
        var query = new Query("CREATE (:Node)");
        var bookmarkValues = new[] { "neo4j:bookmark:v1:tx42" };
        var response = new QueryApiResultSet { Bookmarks = bookmarkValues };

        _mocker.GetMock<IAutoCommitHandler>()
            .Setup(h => h.AutoCommitAsync(query, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mocker.GetMock<IQueryApiResultCursorBuilder>()
            .Setup(b => b.Build(response, query))
            .Returns(Mock.Of<IResultCursor>());

        var session = CreateSession();
        await session.RunAsync(query, null!, false);

        session.LastBookmarks.Values.Should().BeEquivalentTo(bookmarkValues);
    }

    [Fact]
    public async Task BeginTransactionAsync_DelegatesToFactoryWithAllParameters()
    {
        Action<TransactionConfigBuilder> configAction = _ => { };
        var expectedTx = new Mock<IInternalAsyncTransaction>().Object;

        _mocker.GetMock<IQueryApiTransactionFactory>()
            .Setup(f => f.BeginTransactionAsync(
                It.IsAny<AccessMode>(),
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTx);

        var tx = await CreateSession().BeginTransactionAsync(AccessMode.Read, configAction);

        tx.Should().BeSameAs(expectedTx);
        _mocker.GetMock<IQueryApiTransactionFactory>().Verify(
            f => f.BeginTransactionAsync(
                AccessMode.Read,
                configAction,
                Bookmarks.Empty.Values,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteReadAsync_PassesTransactionToWorkAndReturnsResult()
    {
        var tx = new Mock<IInternalAsyncTransaction>();
        tx.Setup(t => t.CommitAsync()).ReturnsAsync(Array.Empty<string>());

        _mocker.GetMock<IQueryApiTransactionFactory>()
            .Setup(f => f.BeginTransactionAsync(
                AccessMode.Read,
                It.IsAny<Action<TransactionConfigBuilder>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);

        IAsyncQueryRunner? capturedRunner = null;
        var result = await CreateSession().ExecuteReadAsync<int>(runner =>
        {
            capturedRunner = runner;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        capturedRunner.Should().BeSameAs(tx.Object);
    }
}
