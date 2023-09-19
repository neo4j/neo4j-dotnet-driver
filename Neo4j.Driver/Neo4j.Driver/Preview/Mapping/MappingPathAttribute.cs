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
/// Instructs the default mapper to use a different field than the property name when mapping a value to the
/// marked property. This attribute does not affect custom-defined mappers. A path may consist of the name of the
/// field to be mapped, or a dot-separated path to a nested field.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MappingPathAttribute : Attribute
{
    /// <summary>
    /// Identifier for the value in the field in the record. If the path is a dot-separated path, then the
    /// first part of the path is the identifier for the entity (or dictionary) field in the record, and the
    /// last part is the identifier within that entity or dictionary.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Instructs the default mapper to use a different field than the property name when mapping a value to the
    /// marked property.
    /// </summary>
    /// <param name="path">
    /// Identifier for the value in the field in the record. If the path is a dot-separated path, then the
    /// first part of the path is the key for the entity (or dictionary) field in the record, and the
    /// last part is the key within that entity or dictionary.
    /// </param>
    public MappingPathAttribute(string path)
    {
        Path = path;
    }
}
