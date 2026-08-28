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
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.Util;

public static class ConcurrentHashSetTests
{
    public class TryAddMethod
    {
        [Fact]
        public void ShouldReportWhetherItemWasInserted()
        {
            var set = new ConcurrentHashSet<string>();

            var first = set.TryAdd("a");
            var second = set.TryAdd("a");

            first.Should().BeTrue();
            second.Should().BeFalse();
            set.Count.Should().Be(1);
        }
    }

    public class TryRemoveMethod
    {
        [Fact]
        public void ShouldReportWhetherItemWasRemoved()
        {
            var set = new ConcurrentHashSet<string>();
            set.TryAdd("a");

            var first = set.TryRemove("a");
            var second = set.TryRemove("a");

            first.Should().BeTrue();
            second.Should().BeFalse();
            set.Count.Should().Be(0);
        }
    }
}
