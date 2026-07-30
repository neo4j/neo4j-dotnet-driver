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

namespace Neo4j.Driver.TestKitBackend.Tests.Logging;

public class LoggingContextTests
{
    [Fact]
    public void Current_is_empty_by_default()
    {
        var context = new LoggingContext();

        context.Current.Should().BeEmpty();
    }

    [Fact]
    public void Set_adds_a_key_value_pair()
    {
        var context = new LoggingContext();

        context.Set("ConnectionId", "testkit-1");

        context.Current["ConnectionId"].Should().Be("testkit-1");
    }

    [Fact]
    public void Set_overwrites_an_existing_key()
    {
        var context = new LoggingContext();

        context.Set("test", "first");
        context.Set("test", "second");

        context.Current["test"].Should().Be("second");
    }

    [Fact]
    public void Remove_removes_a_key()
    {
        var context = new LoggingContext();
        context.Set("test", "some.test");

        context.Remove("test");

        context.Current.ContainsKey("test").Should().BeFalse();
    }

    [Fact]
    public void Remove_of_a_key_that_was_never_set_is_a_no_op()
    {
        var context = new LoggingContext();

        context.Remove("test");

        context.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task Values_set_in_a_child_async_flow_remain_visible_to_the_caller()
    {
        var context = new LoggingContext();

        await Task.Run(() => context.Set("test", "some.test"), TestContext.Current.CancellationToken);

        context.Current["test"].Should().Be("some.test");
    }
}
