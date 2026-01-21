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

using Neo4j.Driver.Mapping;

namespace Neo4j.Driver.Internal.Mapping;

/// <summary>
/// Represents metadata for mapping an entity during the mapping process in the Neo4j driver.
/// Contains information about the path, source, optionality, default value, and
/// explicitness.
/// </summary>
public class MappingBinding
{
    /// <summary>
    /// Represents a mapping binding used to associate a specific path
    /// with a mapping source in Neo4j operations.
    /// </summary>
    /// <param name="path">The path to the data in the source (e.g. a field name in a record or a property name
    /// in a node).</param>
    /// <param name="mappingSource">The source type for the mapping (e.g. Property, Label, Id).</param>
    /// <param name="optional">If <c>true</c>, the mapping will not throw an exception if the source value is
    /// missing.</param>
    internal MappingBinding(string path, MappingSource mappingSource, bool optional = false)
    {
        Path = path;
        MappingSource = mappingSource;
        Optional = optional;
    }

    /// <summary>
    /// Gets or sets the path to the data in the source (e.g. a field name in a record or a property name in a node).
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the source type for the mapping (e.g. Property, Label, Id).
    /// </summary>
    public MappingSource MappingSource { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mapping is optional. 
    /// If <c>true</c>, the mapping will not throw an exception if the source value is missing.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets the default value to be used if the source value is missing and the mapping is optional.
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping was explicitly defined by the user.
    /// </summary>
    public bool Explicit { get; set; }

    /// <summary>
    /// Gets the name of the parameter that will be set when mapping to Cypher parameters.
    /// </summary>
    public string ParameterName { get; set; }
}
