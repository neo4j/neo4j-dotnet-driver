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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// An attribute that applies metadata for mapping bindings in Neo4j object mapping.
/// This attribute is used to customize the mapping of properties or parameters
/// when interacting with the Neo4j database.
/// </summary>
public class MappingBindingsAttribute : Attribute, IMappingBindingMutator
{
    /// <summary>
    /// Gets or sets the path to the data in the source (e.g. a field name in a record or a property name in a node).
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the source type for the mapping (e.g. Property, Label, Id).
    /// </summary>
    public MappingSource? Source { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mapping will not throw an exception if the source value is missing.
    /// </summary>
    public bool? Optional { get; set; }

    /// <summary>
    /// Gets or sets the default value to be used if the source value is missing and the mapping is optional.
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping binding was explicitly defined by the user.
    /// </summary>
    public bool? Explicit { get; set; }

    /// <summary>
    /// Gets or sets the name of the parameter that will be set when mapping to Cypher parameters.
    /// </summary>
    public string ParameterName { get; set; }

    /// <summary>
    /// Represents an attribute that provides metadata for defining custom
    /// mapping bindings in Neo4j object mapping. This attribute is used
    /// to customize how properties or parameters are mapped when interacting
    /// with the Neo4j database.
    /// </summary>
    /// <param name="path">The path to the data in the source (e.g. a field name in a record or a property name
    /// in a node).</param>
    /// <param name="mappingSource">The source type for the mapping (e.g. Property, Label, Id).</param>
    /// <param name="optional">If <c>true</c>, the mapping will not throw an exception if the source value is
    /// missing.</param>
    /// <param name="defaultValue">The default value to be used if the source value is missing and the mapping is optional.</param>
    /// <param name="isExplicit">If <c>true</c>, this mapping binding was explicitly defined by the user.</param>
    /// <param name="parameterName">The name of the parameter that will be set when mapping to Cypher parameters.</param>
    /// <seealso cref="MappingBinding"/>
    public MappingBindingsAttribute(
        string path = null,
        MappingSource? mappingSource = null,
        bool? optional = null,
        object defaultValue = null,
        bool? isExplicit = null,
        string parameterName = null)
    {
        Path = path;
        Source = mappingSource;
        Optional = optional;
        DefaultValue = defaultValue;
        Explicit = isExplicit;
        ParameterName = parameterName;
    }

    /// <inheritdoc />
    public virtual void Mutate(MappingBinding binding)
    {
        binding.Path = Path ?? binding.Path;
        binding.MappingSource = Source ?? binding.MappingSource;
        binding.Optional = Optional ?? binding.Optional;
        binding.DefaultValue = DefaultValue ?? binding.DefaultValue;
        binding.Explicit = Explicit ?? binding.Explicit;
        binding.ParameterName = ParameterName ?? binding.ParameterName;
    }
}
