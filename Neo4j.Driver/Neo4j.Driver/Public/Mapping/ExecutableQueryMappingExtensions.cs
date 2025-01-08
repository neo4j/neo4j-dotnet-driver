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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains extensions for using the global mapping system with the driver's <see cref="ExecutableQuery{TIn,TOut}"/>
/// methods.
/// </summary>
public static class ExecutableQueryMappingExtensions
{
    /// <summary>
    /// Add this method to an <see cref="ExecutableQuery{TIn,TOut}"/> method chain to map the results to objects
    /// as part of the query execution.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <typeparam name="T">The type to map to.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<T>> AsObjectsAsync<T>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(RecordObjectMapping.Map<T>).ToList();
    }

    /// <summary>
    /// Add this method to an <see cref="ExecutableQuery{TIn,TOut}"/> method chain to map the results to objects
    /// as part of the query execution.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="blueprint">The blueprint to use for mapping.</param>
    /// <typeparam name="T">The type to map to.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<T>> AsObjectsFromBlueprintAsync<T>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        T blueprint)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObjectFromBlueprint(blueprint)).ToList();
    }
}
