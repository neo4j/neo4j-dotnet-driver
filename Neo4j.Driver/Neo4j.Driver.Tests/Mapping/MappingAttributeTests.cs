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

using System;
using System.Reflection;
using FluentAssertions;
using Neo4j.Driver.Internal.Mapping;
using Neo4j.Driver.Mapping;
using Xunit;

namespace Neo4j.Driver.Tests.Mapping;

public class MappingAttributeTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static T GetAttribute<T>(string propertyName) where T : Attribute =>
        typeof(Subject).GetProperty(propertyName)!.GetCustomAttribute<T>()!;

    private static MappingBinding DefaultBinding() =>
        new("original_path", EntityMappingSource.Property);

    // ── fixture class decorated with every attribute variant ─────────────────

    private class Subject
    {
        [MappingBindings(
            Path = "mapped_path",
            Source = EntityMappingSource.NodeLabel,
            Optional = true,
            DefaultValue = "fallback",
            Explicit = true,
            CypherParameterName = "param_name")]
        public string AllFields { get; set; }

        [MappingBindings(Path = "only_path")]
        public string OnlyPath { get; set; }

        [MappingBindings(CypherParameterName = "only_param")]
        public string OnlyCypherParam { get; set; }

        [MappingSource("source_path")]
        public string SourcePath { get; set; }

        [MappingSource("source_path_label", EntityMappingSource.NodeLabel)]
        public string SourcePathWithSource { get; set; }

        [MappingOptional]
        public string Optional { get; set; }

        [MappingDefaultValue("default_val")]
        public string DefaultValue { get; set; }

        [CypherParameterMapping("cypher_param")]
        public string CypherParam { get; set; }
    }

    // ── MappingBindingsAttribute ──────────────────────────────────────────────

    [Fact]
    public void MappingBindings_SetsAllFields()
    {
        var attr = GetAttribute<MappingBindingsAttribute>(nameof(Subject.AllFields));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.Path.Should().Be("mapped_path");
        binding.EntityMappingSource.Should().Be(EntityMappingSource.NodeLabel);
        binding.Optional.Should().BeTrue();
        binding.DefaultValue.Should().Be("fallback");
        binding.Explicit.Should().BeTrue();
        binding.CypherParameterName.Should().Be("param_name");
    }

    [Fact]
    public void MappingBindings_UnsetFieldsDoNotOverwriteBinding()
    {
        var attr = GetAttribute<MappingBindingsAttribute>(nameof(Subject.OnlyPath));
        var binding = new MappingBinding("original_path", EntityMappingSource.NodeLabel, optional: true)
        {
            CypherParameterName = "keep_me",
            Explicit = true
        };

        attr.Mutate(binding);

        binding.Path.Should().Be("only_path");
        binding.EntityMappingSource.Should().Be(EntityMappingSource.NodeLabel);   // unchanged
        binding.Optional.Should().BeTrue();                            // unchanged
        binding.CypherParameterName.Should().Be("keep_me");           // unchanged
        binding.Explicit.Should().BeTrue();                            // unchanged
    }

    [Fact]
    public void MappingBindings_OnlyCypherParameterName()
    {
        var attr = GetAttribute<MappingBindingsAttribute>(nameof(Subject.OnlyCypherParam));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.CypherParameterName.Should().Be("only_param");
        binding.Path.Should().Be("original_path"); // unchanged
    }

    // ── MappingSourceAttribute ────────────────────────────────────────────────

    [Fact]
    public void MappingSource_SetsPathAndMarksExplicit()
    {
        var attr = GetAttribute<MappingSourceAttribute>(nameof(Subject.SourcePath));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.Path.Should().Be("source_path");
        binding.Explicit.Should().BeTrue();
        binding.EntityMappingSource.Should().Be(EntityMappingSource.Property); // unchanged
    }

    [Fact]
    public void MappingSource_SetsPathAndMappingSourceAndMarksExplicit()
    {
        var attr = GetAttribute<MappingSourceAttribute>(nameof(Subject.SourcePathWithSource));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.Path.Should().Be("source_path_label");
        binding.EntityMappingSource.Should().Be(EntityMappingSource.NodeLabel);
        binding.Explicit.Should().BeTrue();
    }

    // ── MappingOptionalAttribute ──────────────────────────────────────────────

    [Fact]
    public void MappingOptional_SetsOptional()
    {
        var attr = GetAttribute<MappingOptionalAttribute>(nameof(Subject.Optional));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.Optional.Should().BeTrue();
    }

    // ── MappingDefaultValueAttribute ──────────────────────────────────────────

    [Fact]
    public void MappingDefaultValue_SetsOptionalAndDefaultValue()
    {
        var attr = GetAttribute<MappingDefaultValueAttribute>(nameof(Subject.DefaultValue));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.Optional.Should().BeTrue();
        binding.DefaultValue.Should().Be("default_val");
    }

    // ── CypherParameterMappingAttribute ──────────────────────────────────────

    [Fact]
    public void CypherParameterMapping_SetsCypherParameterName()
    {
        var attr = GetAttribute<CypherParameterMappingAttribute>(nameof(Subject.CypherParam));
        var binding = DefaultBinding();

        attr.Mutate(binding);

        binding.CypherParameterName.Should().Be("cypher_param");
        binding.Path.Should().Be("original_path"); // unchanged
    }
}
