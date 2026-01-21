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
using Neo4j.Driver.Internal.Mapping;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Instructs the default mapper to use a different field than the property name when mapping a value to the
/// marked property. This attribute does not affect custom-defined mappers. A path may consist of the name of the field to
/// be mapped, or a dot-separated path to a nested field.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class MappingSourceAttribute : MappingBindingsAttribute
{
    /// <summary>
    /// Instructs the default mapper to use a different field than the property name when mapping a value to the
    /// marked property.
    /// </summary>
    /// <param name="path">
    /// Identifier for the value in the field in the record. If the path is a dot-separated path, then the
    /// first part of the path is the key for the entity (or dictionary) field in the record, and the last part is the key
    /// within that entity or dictionary.
    /// </param>
    public MappingSourceAttribute(string path)
        : base(path: path)
    {
    }
    /// <summary>
    /// Instructs the default mapper to use a different field than the property name when mapping a value to the
    /// marked property.
    /// </summary>
    /// <param name="key">
    /// Identifier for the value in the field in the record. If the path is a dot-separated path, then the
    /// first part of the path is the key for the entity (or dictionary) field in the record, and the last part is the key
    /// within that entity or dictionary.
    /// </param>
    /// <param name="mappingSource">The source of the value to be mapped.</param>
    public MappingSourceAttribute(string key, MappingSource mappingSource) 
        : base(path: key, mappingSource: mappingSource)
    {
    }

    /// <inheritdoc/>
    public override void Mutate(MappingBinding binding)
    {
        base.Mutate(binding);
        binding.Explicit = true;
    }
}
