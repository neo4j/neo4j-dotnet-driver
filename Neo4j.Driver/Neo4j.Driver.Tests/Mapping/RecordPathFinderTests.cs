// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping
{
    public class RecordPathFinderTests
    {
        [Fact]
        public void ShouldFindSimplePath()
        {
            var record = new Record(new[] { "a" }, new object[] { "b" });
            var finder = new RecordPathFinder();

            var found = finder.TryGetValueByPath(record, "a", out var value);

            found.Should().BeTrue();
            value.Should().Be("b");
        }

        [Fact]
        public void ShouldReturnFalseWhenPathNotFound()
        {
            var record = new Record(new[] { "a" }, new object[] { "b" });
            var finder = new RecordPathFinder();

            var found = finder.TryGetValueByPath(record, "c", out var value);

            found.Should().BeFalse();
        }

        [Fact]
        public void ShouldFindSimplePathNestedInANode()
        {
            var node = new Node(1, new[] { "Test" }, new Dictionary<string, object>() { { "name", "Bob" } });
            var record = new Record(new[] { "person" }, new object[] { node });
            var finder = new RecordPathFinder();

            var found = finder.TryGetValueByPath(record, "name", out var value);

            found.Should().BeTrue();
            value.Should().Be("Bob");
        }

        [Fact]
        public void ShouldFindComplexPathNestedInANode()
        {
            var node = new Node(1, new[] { "Test" }, new Dictionary<string, object>() { { "name", "Bob" } });
            var record = new Record(new[] { "person" }, new object[] { node });
            var finder = new RecordPathFinder();

            var found = finder.TryGetValueByPath(record, "person.name", out var value);

            found.Should().BeTrue();
            value.Should().Be("Bob");
        }

        [Fact]
        public void ShouldFindComplexPathNestedInADictionary()
        {
            var dictionary = new Dictionary<string, object>() { { "name", "Bob" } };
            var record = new Record(new[] { "person" }, new object[] { dictionary });
            var finder = new RecordPathFinder();

            var found = finder.TryGetValueByPath(record, "person.name", out var value);

            found.Should().BeTrue();
            value.Should().Be("Bob");
        }
    }
}
