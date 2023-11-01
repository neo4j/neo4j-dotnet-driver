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
    public class MappingSourceDelegateBuilderTests
    {
        [Fact]
        public void ShouldGetSimplePaths()
        {
            var record = new Record(new[] { "a" }, new object[] { "b" });
            var getter = new MappingSourceDelegateBuilder();
            var mappingSource = new EntityMappingInfo("a", EntityMappingSource.Property);

            var mappingDelegate = getter.GetMappingDelegate(mappingSource);
            var found = mappingDelegate(record, out var value);

            found.Should().BeTrue();
            value.Should().Be("b");
        }

        [Fact]
        public void ShouldReturnFalseWhenPathNotFound()
        {
            var record = new Record(new[] { "a" }, new object[] { "b" });
            var getter = new MappingSourceDelegateBuilder();
            var mappingSource = new EntityMappingInfo("c", EntityMappingSource.Property);

            var mappingDelegate = getter.GetMappingDelegate(mappingSource);
            var found = mappingDelegate(record, out var value);

            found.Should().BeFalse();
        }

        [Fact]
        public void ShouldGetNodeLabels()
        {
            var node = new Node(1, new[] { "Actor", "Director" }, new Dictionary<string, object>());
            var record = new Record(new[] { "a" }, new object[] { node });
            var getter = new MappingSourceDelegateBuilder();
            var mappingSource = new EntityMappingInfo("a", EntityMappingSource.NodeLabel);

            var mappingDelegate = getter.GetMappingDelegate(mappingSource);
            var found = mappingDelegate(record, out var value);

            found.Should().BeTrue();
            value.Should().BeEquivalentTo(new[] { "Actor", "Director" });
        }

        [Fact]
        public void ShouldGetRelationshipType()
        {
            var rel = new Relationship(1, 2, 3, "ACTED_IN", new Dictionary<string, object>());
            var record = new Record(new[] { "a" }, new object[] { rel });
            var getter = new MappingSourceDelegateBuilder();
            var mappingSource = new EntityMappingInfo("a", EntityMappingSource.RelationshipType);

            var mappingDelegate = getter.GetMappingDelegate(mappingSource);
            var found = mappingDelegate(record, out var value);

            found.Should().BeTrue();
            value.Should().Be("ACTED_IN");
        }
    }
}
