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

namespace Neo4j.Driver.Mapping;

/// <summary>Represents a mapping from an entity itself rather than any of its properties.</summary>
public enum MappingSource
{
    /// <summary>The value of the specified property will be used as the value.</summary>
    Property,

    /// <summary>
    /// If the value of the specified property is a relationship, then the relationship type will be used as the
    /// value. Otherwise, the property will be ignored.
    /// </summary>
    RelationshipType,

    /// <summary>
    /// If the value of the specified property is a node, then the labels will be used as the value. If the
    /// destination property is a string, then the labels will be joined with a comma. If the destination property is a list,
    /// then the labels will be added to the list. Otherwise, the property will be ignored.
    /// </summary>
    NodeLabel
}
