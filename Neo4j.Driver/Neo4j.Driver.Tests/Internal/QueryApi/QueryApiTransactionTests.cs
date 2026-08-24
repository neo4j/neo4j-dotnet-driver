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

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Tests.Internal.Core;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiTransactionTests
{
    private const string TxId = "tx-1";

    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiTransaction>();

    public QueryApiTransactionTests()
    {
        _autoMocker.GetMock<IQueryApiTransactionContextTracker>()
            .SetupGet(x => x.Context)
            .Returns(new QueryApiTransactionContext(TxId, null));

        _autoMocker.GetMock<ILoggingContextTracker>()
            .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Mock.Of<IDisposable>());
    }

    [Fact]
    public void TransactionConfig_ReturnsDefault()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();

        subject.TransactionConfig.Should().BeSameAs(TransactionConfig.Default);
    }

    [Fact]
    public async Task RunAsync_ReturnsCursorFromRunner()
    {
        var query = new Query("RETURN 1");
        var expectedCursor = Mock.Of<IResultCursor>();

        _autoMocker.GetMock<ITransactionRunner>()
            .Setup(r => r.RunAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCursor);

        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        var result = await subject.RunAsync(query);

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task RunAsync_String_DelegatesToQueryOverload()
    {
        var expectedCursor = Mock.Of<IResultCursor>();

        _autoMocker.GetMock<ITransactionRunner>()
            .Setup(r => r.RunAsync(It.Is<Query>(q => q.Text == "RETURN 1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCursor);

        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        var result = await subject.RunAsync("RETURN 1");

        result.Should().BeSameAs(expectedCursor);
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterCommit()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.CommitAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RollbackAsync_ThrowsAfterCommit()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.RollbackAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterCommit()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.CommitAsync();

        var act = () => subject.RunAsync(new Query("RETURN 1"));
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task RunAsync_ThrowsAfterRollback()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.RollbackAsync();

        var act = () => subject.RunAsync(new Query("RETURN 1"));
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public async Task CommitAsync_ThrowsAfterRollback()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.RollbackAsync();

        var act = () => subject.CommitAsync();
        await act.Should().ThrowAsync<TransactionClosedException>();
    }

    [Fact]
    public void IsOpen_TrueInitially()
    {
        _autoMocker.CreateInstance<QueryApiTransaction>().IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task IsOpen_FalseAfterCommit()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.CommitAsync();

        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task IsOpen_FalseAfterRollback()
    {
        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.RollbackAsync();

        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenOpen_RollsBack()
    {
        var rollbackCalled = false;

        _autoMocker.GetMock<ITransactionRollback>()
            .Setup(h => h.RollbackAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCalled = true)
            .Returns(Task.CompletedTask);

        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.DisposeAsync();

        rollbackCalled.Should().BeTrue();
        subject.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenCommitted_DoesNotRollBack()
    {
        var rollbackCalled = false;

        _autoMocker.GetMock<ITransactionRollback>()
            .Setup(h => h.RollbackAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCalled = true)
            .Returns(Task.CompletedTask);

        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();
        await subject.CommitAsync();
        await subject.DisposeAsync();

        rollbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenAlreadyRolledBack_DoesNotRollBackAgain()
    {
        var rollbackCount = 0;

        _autoMocker.GetMock<ITransactionRollback>()
            .Setup(h => h.RollbackAsync(It.IsAny<CancellationToken>()))
            .Callback(() => rollbackCount++)
            .Returns(Task.CompletedTask);

        var subject = _autoMocker.CreateInstance<QueryApiTransaction>();

        await subject.RollbackAsync();
        await subject.DisposeAsync();

        rollbackCount.Should().Be(1);
    }
}
