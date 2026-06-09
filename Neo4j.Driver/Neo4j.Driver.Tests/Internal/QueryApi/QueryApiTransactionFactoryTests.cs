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
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiTransactionFactoryTests
{
    private readonly QueryApiTransactionContextHolder _contextHolder = new();
    private readonly Mock<IResolutionScope> _sessionScope = new();
    private readonly Mock<ITransactionBeginner> _transactionStarter = new();
    private readonly Mock<IResolutionScope> _txScope = new();

    public QueryApiTransactionFactoryTests()
    {
        _sessionScope
            .Setup(s => s.CreateChildScope(It.IsAny<Action<IServiceRegistry>>()))
            .Returns(_txScope.Object);
    }

    private QueryApiTransactionFactory CreateFactory() =>
        new(_transactionStarter.Object, _contextHolder, _sessionScope.Object, new Mock<ILogger>().Object);

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionResolvedFromChildScope()
    {
        var context = new QueryApiTransactionContext("tx-1", null);
        var expectedTx = new Mock<IInternalAsyncTransaction>().Object;

        _transactionStarter
            .Setup(s => s.BeginAsync(default))
            .Callback(() => _contextHolder.Set(context));

        _txScope.Setup(s => s.Resolve<IInternalAsyncTransaction>()).Returns(expectedTx);

        var result = await CreateFactory().BeginTransactionAsync(AccessMode.Write, null);

        result.Should().BeSameAs(expectedTx);
    }

    [Fact]
    public async Task BeginTransactionAsync_Throws_WhenContextHolderIsEmpty()
    {
        _transactionStarter.Setup(s => s.BeginAsync(default));

        var factory = CreateFactory();
        var act = () => factory.BeginTransactionAsync(AccessMode.Write, null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*context*");
    }
}
