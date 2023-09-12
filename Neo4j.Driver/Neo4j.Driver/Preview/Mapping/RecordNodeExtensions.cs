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

namespace Neo4j.Driver.Preview.Mapping;

/// <summary>
/// Contains extensions for accessing values simply from records and nodes.
/// </summary>
public static class RecordNodeExtensions
{
    /// <summary>
    /// Converts the record to an object of the given type according to the global mapping configuration.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <typeparam name="T">The type to map to.</typeparam>
    /// <returns>The mapped object.</returns>
    public static T AsObject<T>(this IRecord record) where T : new()
    {
        return RecordObjectMapping.Map<T>(record);
    }

    /// <summary>
    /// Gets the value of the given key from the record, converting it to the given type.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    public static T GetValue<T>(this IRecord record, string key)
    {
        return record[key].As<T>();
    }

    /// <summary>
    /// Gets the <see cref="INode"/> identified by the given key from the record.
    /// </summary>
    /// <param name="record">The record to get the node from.</param>
    /// <param name="key">The key of the node.</param>
    /// <returns>The node.</returns>
    public static INode GetNode(this IRecord record, string key)
    {
        return record.GetValue<INode>(key);
    }

    /// <summary>
    /// Gets the value of the given key from the node, converting it to the given type.
    /// </summary>
    /// <param name="node">The node to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    public static T GetValue<T>(this INode node, string key)
    {
        return node[key].As<T>();
    }
}
