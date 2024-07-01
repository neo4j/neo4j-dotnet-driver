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

/// <summary>
/// Contains extensions for entities such as nodes and relationships.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Converts the entity to a record.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>The record.</returns>
    public static IRecord AsRecord(this IEntity entity)
    {
        return new DictAsRecord(entity, null);
    }
}
