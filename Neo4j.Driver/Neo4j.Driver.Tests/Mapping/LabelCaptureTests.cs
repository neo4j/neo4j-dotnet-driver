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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Mapping;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class LabelCaptureTests
{
    public LabelCaptureTests()
    {
        RecordObjectMapping.Reset();
    }

    [Fact]
    public void ShouldCaptureSingleNodeLabel()
    {
        var node = new Node(1, new[] { "Test" }, new Dictionary<string, object>());
        var record = TestRecord.Create(new[] { "Person" }, new object[] { node });

        var mapped = record.AsObject<TestMappedClass>();

        mapped.Label.Should().Be("Test");
    }

    [Fact]
    public void ShouldCaptureMultipleNodeLabelsIntoString()
    {
        var node = new Node(1, new[] { "Alpha", "Bravo", "Charlie" }, new Dictionary<string, object>());
        var record = TestRecord.Create(new[] { "Person" }, new object[] { node });

        var mapped = record.AsObject<TestMappedClass>();

        mapped.Label.Should().Be("Alpha,Bravo,Charlie");
    }

    [Fact]
    public void ShouldCaptureRelationshipType()
    {
        var node = new Relationship(1, 2, 3, "ACTED_IN", new Dictionary<string, object>());
        var record = TestRecord.Create(new[] { "Relationship" }, new object[] { node });

        var mapped = record.AsObject<TestMappedClass>();

        mapped.RelationshipType.Should().Be("ACTED_IN");
    }

    [Fact]
    public void ShouldCaptureAndConvertLabels()
    {
        RecordObjectMapping.RegisterProvider<CustomMapper>();
        var node = new Node(1, new[] { "Alpha", "Bravo", "Charlie" }, new Dictionary<string, object>());
        var record = TestRecord.Create(new[] { "Person" }, new object[] { node });

        var mapped = record.AsObject<TestMappedClass>();

        mapped.Label.Should().Be("ALPHA|BRAVO|CHARLIE");
        mapped.Labels.Should().BeEquivalentTo("Alphx", "Brxvo", "Chxrlie");
    }

    [Fact]
    public void ShouldCaptureAndConvertRelationshipType()
    {
        RecordObjectMapping.RegisterProvider<CustomMapper>();
        var node = new Relationship(1, 2, 3, "ACTED_IN", new Dictionary<string, object>());
        var record = TestRecord.Create(new[] { "Relationship" }, new object[] { node });

        var mapped = record.AsObject<TestMappedClass>();

        mapped.RelationshipType.Should().Be("acted_in");
    }

    public class TestMappedClass
    {
        [MappingSource("Person", EntityMappingSource.NodeLabel)]
        [MappingOptional]
        public string Label { get; set; }

        [MappingSource("Person", EntityMappingSource.NodeLabel)]
        [MappingOptional]
        public List<string> Labels { get; set; }

        [MappingSource("Relationship", EntityMappingSource.RelationshipType)]
        [MappingOptional]
        public string RelationshipType { get; set; }
    }

    private class CustomMapper : IMappingProvider
    {
        /// <inheritdoc/>
        public void CreateMappers(IMappingRegistry registry)
        {
            registry.RegisterMapping<TestMappedClass>(
                b => b
                    .Map(
                        x => x.Label,
                        "Person",
                        EntityMappingSource.NodeLabel,
                        x => string.Join("|", ((string[])x).Select(y => y.ToUpper())),
                        true)
                    .Map(
                        x => x.Labels,
                        "Person",
                        EntityMappingSource.NodeLabel,
                        x => ((string[])x).Select(y => y.Replace("a", "x")).ToList(),
                        true)
                    .Map(
                        x => x.RelationshipType,
                        "Relationship",
                        EntityMappingSource.RelationshipType,
                        x => x?.ToString()?.ToLower(),
                        true));
        }
    }
}
