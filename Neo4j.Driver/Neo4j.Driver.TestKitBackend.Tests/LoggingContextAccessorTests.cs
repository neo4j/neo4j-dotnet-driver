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

using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Logging;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class LoggingContextAccessorTests
{
    [Fact]
    public void GetCurrent_returns_null_when_nothing_is_published()
    {
        var accessor = new LoggingContextAccessor();

        accessor.GetCurrent().Should().BeNull();
    }

    [Fact]
    public async Task A_published_context_is_visible_in_child_async_flows()
    {
        var accessor = new LoggingContextAccessor();
        var context = new LoggingContext();

        accessor.Publish(context);
        var seenInChild = await Task.Run(
            () => accessor.GetCurrent(),
            TestContext.Current.CancellationToken);

        seenInChild.Should().BeSameAs(context);
    }

    [Fact]
    public async Task Concurrent_flows_each_see_their_own_published_context()
    {
        var accessor = new LoggingContextAccessor();

        var flows = Enumerable.Range(0, 2).Select(_ => Task.Run(
            async () =>
            {
                var context = new LoggingContext();
                accessor.Publish(context);
                await Task.Delay(20, TestContext.Current.CancellationToken);
                return ReferenceEquals(accessor.GetCurrent(), context);
            },
            TestContext.Current.CancellationToken));

        var results = await Task.WhenAll(flows);

        results.Should().AllBeEquivalentTo(true);
    }
}
