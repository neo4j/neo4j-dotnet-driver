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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains extensions for accessing values simply from records and entities.
/// </summary>
public static class RecordExtensions
{
    /// <summary>
    /// Converts the record to an object of the given type according to the global mapping configuration.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <param name="record">The record to convert.</param>
    /// <typeparam name="T">The type to map to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record)
    {
        return RecordObjectMapping.Map<T>(record);
    }

    /// <summary>
    /// Converts the record to an object of the same type as the given blueprint according to the global
    /// mapping configuration.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <param name="blueprint">An object to be used as a blueprint for the mapping. This could be an object of an
    /// anonymous type.</param>
    /// <typeparam name="T">The type that will be mapped to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record, T blueprint)
    {
        return (T)RecordObjectMapping.Map(record, typeof(T));
    }
}
