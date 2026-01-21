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
    private readonly string _path;
    private readonly MappingSource? _mappingSource;
    private readonly bool? _optional;
    private readonly object _defaultValue;
    private readonly bool? _explicit;
    private readonly string _parameterName;

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
        _path = path;
        _mappingSource = mappingSource;
        _optional = optional;
        _defaultValue = defaultValue;
        _explicit = isExplicit;
        _parameterName = parameterName;
    }

    /// <inheritdoc />
    public virtual void Mutate(MappingBinding binding)
    {
        binding.Path = _path ?? binding.Path;
        binding.MappingSource = _mappingSource ?? binding.MappingSource;
        binding.Optional = _optional ?? binding.Optional;
        binding.DefaultValue = _defaultValue ?? binding.DefaultValue;
        binding.Explicit = _explicit ?? binding.Explicit;
        binding.ParameterName = _parameterName ?? binding.ParameterName;
    }
}
