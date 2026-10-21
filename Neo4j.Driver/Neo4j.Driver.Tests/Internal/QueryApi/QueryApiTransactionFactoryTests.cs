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

public class QueryApiTransactionFactoryTests
{
    private readonly AutoMocker _autoMocker = AutoMockerExtensions.ForTesting<QueryApiTransaction>();
    private readonly Mock<IQueryApiTransactionScope> _transactionScope = new();
    private readonly QueryApiTransaction _transaction;

    public QueryApiTransactionFactoryTests()
    {
        _autoMocker.GetMock<ITransactionRollback>()
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _transaction = _autoMocker.CreateInstance<QueryApiTransaction>();

        _transactionScope.Setup(s => s.Transaction()).Returns(_transaction);
    }

    private Mock<ITransactionBeginner> Beginner => _autoMocker.GetMock<ITransactionBeginner>();

    private QueryApiTransactionFactory CreateFactory()
    {
        return new QueryApiTransactionFactory(() => _transactionScope.Object, new TestLogger(typeof(QueryApiTransactionFactory)));
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionFromTheTransactionScope()
    {
        var result = await CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        result.Should().BeSameAs(_transaction);
    }

    [Fact]
    public async Task BeginTransactionAsync_BeginsTheTransaction()
    {
        await CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        Beginner.Verify(b => b.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposingTheTransaction_DisposesTheTransactionScope()
    {
        var transaction = await CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        await transaction.DisposeAsync();

        _transactionScope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_DoesNotDisposeTheTransactionScope_WhileTheTransactionIsOpen()
    {
        await CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        _transactionScope.Verify(s => s.Dispose(), Times.Never);
    }

    [Fact]
    public async Task BeginTransactionAsync_DisposesTheTransactionScope_WhenBeginFails()
    {
        Beginner
            .Setup(b => b.BeginAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientException("Neo.ClientError.Statement.SyntaxError", "nope"));

        var act = () => CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        await act.Should().ThrowAsync<ClientException>();
        _transactionScope.Verify(s => s.Dispose(), Times.Once);
    }
}
