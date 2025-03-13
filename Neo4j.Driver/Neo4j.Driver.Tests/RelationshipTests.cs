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
using Moq;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests;

public class RelationshipTests
{
    [Fact]
    public void ShouldEqualIfIdEquals()
    {
        var rel1 = new Relationship(123, 000, 111, "buibui", null);
        var rel2 = new Relationship(123, 222, 333, "lala", null);
        rel1.Equals(rel2).Should().BeTrue();
        Equals(rel1, rel2).Should().BeTrue();
        rel1.GetHashCode().Should().Be(rel2.GetHashCode());

        var rel3Mock = new Mock<IRelationship>();
        rel3Mock.Setup(f => f.ElementId).Returns("123");
        rel3Mock.Setup(f => f.StartNodeElementId).Returns("444");
        rel3Mock.Setup(f => f.EndNodeElementId).Returns("555");
        rel3Mock.Setup(f => f.Type).Returns("same interface, different implementation");
        rel3Mock.Setup(f => f.GetHashCode()).Returns(123);
        var rel3 = rel3Mock.Object;

        rel1.Equals(rel3).Should().BeTrue();
        Equals(rel1, rel3).Should().BeTrue();
        // TODO: The following test is currently not supported by Moq
        //rel1.GetHashCode().Should().Be(rel3.GetHashCode());
    }
}
