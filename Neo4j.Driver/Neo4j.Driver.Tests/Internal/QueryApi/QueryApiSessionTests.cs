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
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiSessionTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new QueryApiCustomization());

    [Fact]
    public async Task RunAsync_ReturnsCursorFromRunner()
    {
        var query = new Query("RETURN 1");
        var expectedCursor = Mock.Of<IResultCursor>();

        _fixture.Freeze<Mock<IAutoCommitRunner>>()
            .Setup(r => r.RunAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCursor);

        var sut = _fixture.Create<QueryApiSession>();
        var result = await sut.RunAsync(query, null!, false);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionFromFactory()
    {
        var tx = Mock.Of<IInternalAsyncTransaction>();
        _fixture.Freeze<Mock<IQueryApiTransactionFactory>>()
            .Setup(f => f.BeginTransactionAsync(
                AccessMode.Read,
                It.IsAny<Action<TransactionConfigBuilder>>(),
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
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx.Object);
        var sut = _fixture.Create<QueryApiSession>();

        var result = await sut.ExecuteReadAsync<int>(
            runner => Task.FromResult(ReferenceEquals(runner, tx.Object) ? 42 : -1));

        result.Should().Be(42);
    }
}
