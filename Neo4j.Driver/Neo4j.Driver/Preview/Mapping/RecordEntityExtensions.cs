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
/// Contains extensions for accessing values simply from records and entities.
/// </summary>
public static class RecordEntityExtensions
{
    /// <summary>
    /// Converts the record to an object of the given type according to the global mapping configuration.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
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
    public static T? GetValue<T>(this IRecord record, string key)
    {
        return record.Values.TryGetValue(key, out var value) ? value.As<T>() : default;
    }

    /// <summary>
    /// Gets the <see cref="IEntity"/> identified by the given key from the record.
    /// </summary>
    /// <param name="record">The record to get the entity from.</param>
    /// <param name="key">The key of the entity.</param>
    /// <returns>The entity.</returns>
    public static IEntity GetEntity(this IRecord record, string key)
    {
        return record.GetValue<IEntity>(key);
    }

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to the given type.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    public static T? GetValue<T>(this IEntity entity, string key)
    {
        return entity.Properties.TryGetValue(key, out var value) ? value.As<T>() : default;
    }

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a string.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static string GetString(this IRecord record, string key) => record.GetValue<string>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to an int.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static int GetInt(this IRecord record, string key) => record.GetValue<int>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a long.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static long GetLong(this IRecord record, string key) => record.GetValue<long>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a double.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static double GetDouble(this IRecord record, string key) => record.GetValue<double>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a float.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static float GetFloat(this IRecord record, string key) => record.GetValue<float>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a bool.
    /// </summary>
    /// <param name="record">The record to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static bool GetBool(this IRecord record, string key) => record.GetValue<bool>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a string.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static string GetString(this IEntity entity, string key) => entity.GetValue<string>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to an int.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static int GetInt(this IEntity entity, string key) => entity.GetValue<int>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a long.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static long GetLong(this IEntity entity, string key) => entity.GetValue<long>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a double.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static double GetDouble(this IEntity entity, string key) => entity.GetValue<double>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a float.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static float GetFloat(this IEntity entity, string key) => entity.GetValue<float>(key);

    /// <summary>
    /// Gets the value of the given key from the entity, converting it to a bool.
    /// </summary>
    /// <param name="entity">The entity to get the value from.</param>
    /// <param name="key">The key of the value.</param>
    /// <returns>The converted value.</returns>
    public static bool GetBool(this IEntity entity, string key) => entity.GetValue<bool>(key);
}
