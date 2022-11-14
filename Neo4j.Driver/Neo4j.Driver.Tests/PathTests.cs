// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests;

public class PathTests
{
    [Fact]
    public void ShouldEqualIfIdEquals()
    {
        var path1 = new Path(
            null,
            new[] { new Node(123, new[] { "buibui" }, null) },
            new[] { new Relationship(1, 000, 111, "buibui", null) });

        var path2 = new Path(
            null,
            new[] { new Node(123, new[] { "lala" }, null) },
            new[] { new Relationship(1, 222, 333, "lala", null) });

        path1.Equals(path2).Should().BeTrue();
        Equals(path1, path2).Should().BeTrue();
        path1.GetHashCode().Should().Be(path2.GetHashCode());

        var path3Mock = new Mock<IPath>();
        path3Mock.Setup(f => f.Start)
            .Returns(new Node(123, new[] { "same interface, different implementation" }, null));

        path3Mock.Setup(f => f.Relationships)
            .Returns(new[] { new Relationship(1, 222, 333, "same interface --- different implementation", null) });

        var path3 = path3Mock.Object;
        path1.Equals(path3).Should().BeTrue();
        Equals(path1, path3).Should().BeTrue();
    }
}
