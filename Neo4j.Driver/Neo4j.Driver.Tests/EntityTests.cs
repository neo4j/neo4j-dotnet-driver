//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class EntityTests
    {
        public class NodeTests
        {
            [Fact]
            public void ShouldEqualIfIdEquals()
            {
                var node1 = new Node(123, new []{"buibui"}, null);
                var node2 = new Node(123, new []{"lala"}, null);
                node1.Equals(node2).Should().BeTrue();
                Equals(node1, node2).Should().BeTrue();
                node1.GetHashCode().Should().Be(node2.GetHashCode());
            }
        }

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
            }
        }

        public class PathTests
        {
            [Fact]
            public void ShouldEqualIfIdEquals()
            {
                var path1 = new Path(null, 
                    new []{ new Node(123, new []{"buibui"}, null) },
                    new []{ new Relationship(1, 000, 111, "buibui", null)});
                var path2 = new Path(null, 
                    new[] { new Node(123, new[] { "lala" }, null) },
                    new []{ new Relationship(1, 222, 333, "lala", null)});
                path1.Equals(path2).Should().BeTrue();
                Equals(path1, path2).Should().BeTrue();
                path1.GetHashCode().Should().Be(path2.GetHashCode());
            }
        }
    }
}
