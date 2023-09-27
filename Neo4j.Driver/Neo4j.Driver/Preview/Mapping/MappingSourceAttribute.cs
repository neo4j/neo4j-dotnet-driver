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

using System;

namespace Neo4j.Driver.Preview.Mapping;

/// <summary>
/// Represents a mapping from an entity itself rather than any of its properties.
/// </summary>
public enum EntityMappingSource
{
    /// <summary>
    /// The value of the specified property will be used as the value.
    /// </summary>
    Property,

    /// <summary>
    /// If the value of the specified property is a relationship, then the relationship type will be used as the value.
    /// Otherwise, the property will be ignored.
    /// </summary>
    RelationshipType,

    /// <summary>
    /// If the value of the specified property is a node, then the labels will be used as the value. If the destination
    /// property is a string, then the labels will be joined with a comma. If the destination property is a list, then
    /// the labels will be added to the list. Otherwise, the property will be ignored.
    /// </summary>
    NodeLabel
}

internal record MappingSource(string Path, EntityMappingSource EntityMappingSource);

/// <summary>
/// Instructs the default mapper to use a different field than the property name when mapping a value to the
/// marked property. This attribute does not affect custom-defined mappers. A path may consist of the name of the
/// field to be mapped, or a dot-separated path to a nested field.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MappingSourceAttribute : Attribute
{
    internal MappingSource MappingSource { get; }

    /// <summary>
    /// Instructs the default mapper to use a different field than the property name when mapping a value to the
    /// marked property.
    /// </summary>
    /// <param name="path">
    /// Identifier for the value in the field in the record. If the path is a dot-separated path, then the
    /// first part of the path is the key for the entity (or dictionary) field in the record, and the
    /// last part is the key within that entity or dictionary.
    /// </param>
    public MappingSourceAttribute(string path)
    {
        MappingSource = new MappingSource(path, EntityMappingSource.Property);
    }

    public MappingSourceAttribute(string key, EntityMappingSource entityMappingSource)
    {
        MappingSource = new MappingSource(key, entityMappingSource);
    }
}
