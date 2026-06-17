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
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiContainerBuilderTests
{
    /// <summary>
    /// Mirrors <c>GraphDatabase.GetQueryApiDriver</c>: build the driver-scope container and resolve the root
    /// object the driver needs (<see cref="IQueryApiProtocolAdapter"/>). Resolving the adapter walks the whole
    /// driver-scope graph, so this is the wiring smoke test that catches registration cycles and missing
    /// dependencies before they surface at <c>GraphDatabase.Driver()</c> time.
    /// </summary>
    [Fact]
    public void BuildContainer_ResolvesProtocolAdapter()
    {
        var context = new DriverContext(
            new Uri("http://localhost"),
            Mock.Of<IAuthTokenManager>(),
            new Config());

        var container = new QueryApiContainerBuilder().BuildContainer(context);

        container.Resolve<IQueryApiProtocolAdapter>().Should().NotBeNull();
    }
}
