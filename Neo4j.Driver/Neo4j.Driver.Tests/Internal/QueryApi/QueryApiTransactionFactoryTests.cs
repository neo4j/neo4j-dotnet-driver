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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi;
using Neo4j.Driver.Internal.QueryApi.Abstractions;
using Neo4j.Driver.Internal.QueryApi.Implementations;
using Neo4j.Driver.Internal.QueryApi.Types;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiTransactionFactoryTests
{
    private readonly Mock<IBeginTransactionHandler> _beginHandler = new();
    private readonly Mock<IResolutionScope> _sessionScope = new();
    private readonly Mock<IResolutionScope> _txScope = new();

    public QueryApiTransactionFactoryTests()
    {
        _sessionScope
            .Setup(s => s.CreateChildScope(It.IsAny<Action<IServiceRegistry>>()))
            .Returns(_txScope.Object);
    }

    private QueryApiTransactionFactory CreateFactory() =>
        new(_beginHandler.Object, _sessionScope.Object, new Mock<ILogger>().Object);

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionResolvedFromChildScope()
    {
        var context = new QueryApiTransactionContext("tx-1", null);
        var expectedTx = new Mock<IInternalAsyncTransaction>().Object;

        _beginHandler.Setup(h => h.BeginTransactionAsync(It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync(context);

        _txScope.Setup(s => s.Resolve<IInternalAsyncTransaction>()).Returns(expectedTx);

        var result = await CreateFactory().BeginTransactionAsync(AccessMode.Write, null, []);

        result.Should().BeSameAs(expectedTx);
    }

}
