﻿// Copyright (c) "Neo4j"
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
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.Preview.Mapping;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests.Mapping
{
    public class LabelCaptureTests
    {
        public class TestMappedClass
        {
            [MappingSource("Person", EntityMappingSource.NodeLabel)]
            public string Label { get; set; }

            [MappingSource("Person", EntityMappingSource.NodeLabel)]
            public List<string> Labels { get; set; }

            [MappingSource("Relationship", EntityMappingSource.RelationshipType)]
            public string RelationshipType { get; set; }
        }

        public LabelCaptureTests()
        {
            RecordObjectMapping.Reset();
        }

        [Fact]
        public void ShouldCaptureSingleNodeLabel()
        {
            var node = new Node(1, new[] { "Test" }, new Dictionary<string, object>());
            var record = new Record(new[] { "Person" }, new object[] { node });

            var mapped = record.AsObject<TestMappedClass>();

            mapped.Label.Should().Be("Test");
        }

        [Fact]
        public void ShouldCaptureMultipleNodeLabelsIntoString()
        {
            var node = new Node(1, new[] { "Alpha", "Bravo", "Charlie" }, new Dictionary<string, object>());
            var record = new Record(new[] { "Person" }, new object[] { node });

            var mapped = record.AsObject<TestMappedClass>();

            mapped.Label.Should().Be("Alpha,Bravo,Charlie");
        }

        [Fact]
        public void ShouldCaptureRelationshipType()
        {
            var node = new Relationship(1, 2, 3, "ACTED_IN", new Dictionary<string, object>());
            var record = new Record(new[] { "Relationship" }, new object[] { node });

            var mapped = record.AsObject<TestMappedClass>();

            mapped.RelationshipType.Should().Be("ACTED_IN");
        }

        class CustomMapper : IMappingProvider
        {
            /// <inheritdoc />
            public void CreateMappers(IMappingRegistry registry)
            {
                registry.RegisterMapping<TestMappedClass>(
                    b => b
                        .Map(
                            x => x.Label,
                            "Person",
                            EntityMappingSource.NodeLabel,
                            x => string.Join("|", ((string[])x).Select(y => y.ToUpper())))
                        .Map(
                            x => x.Labels,
                            "Person",
                            EntityMappingSource.NodeLabel,
                            x => ((string[])x).Select(y => y.Replace("a", "x")).ToList())
                        .Map(
                            x => x.RelationshipType,
                            "Relationship",
                            EntityMappingSource.RelationshipType,
                            x => x?.ToString()?.ToLower()));
            }
        }

        [Fact]
        public void ShouldCaptureAndConvertLabels()
        {
            RecordObjectMapping.RegisterProvider<CustomMapper>();
            var node = new Node(1, new[] { "Alpha", "Bravo", "Charlie" }, new Dictionary<string, object>());
            var record = new Record(new[] { "Person" }, new object[] { node });

            var mapped = record.AsObject<TestMappedClass>();

            mapped.Label.Should().Be("ALPHA|BRAVO|CHARLIE");
            mapped.Labels.Should().BeEquivalentTo("Alphx", "Brxvo", "Chxrlie");
        }

        [Fact]
        public void ShouldCaptureAndConvertRelationshipType()
        {
            RecordObjectMapping.RegisterProvider<CustomMapper>();
            var node = new Relationship(1, 2, 3, "ACTED_IN", new Dictionary<string, object>());
            var record = new Record(new[] { "Relationship" }, new object[] { node });

            var mapped = record.AsObject<TestMappedClass>();

            mapped.RelationshipType.Should().Be("acted_in");
        }
    }
}
