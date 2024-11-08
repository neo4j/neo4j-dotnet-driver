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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Contains extensions for using delegate mapping with the driver's <see cref="ExecutableQuery{TIn,TOut}"/>
/// methods.
/// </summary>
public static class DelegateExecutableQueryMappingExtensions
{
    /// <summary>
    /// Add this method to an <see cref="ExecutableQuery{TIn,TOut}"/> method chain to map the results to objects
    /// using a delegate as part of the query execution.
    /// </summary>
    /// <seealso cref="RecordObjectMapping.Map{T}"/>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5, T6>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, T6, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter to the mapping function.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter to the mapping function.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter to the mapping function.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter to the mapping function.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter to the mapping function.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter to the mapping function.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }

    /// <summary>
    /// Maps the results to objects using a delegate as part of the query execution.
    /// </summary>
    /// <param name="recordsTask">The task that will return the records.</param>
    /// <param name="map">The delegate to use to map the records to objects.</param>
    /// <typeparam name="TResult">The type to map to.</typeparam>
    /// <typeparam name="T1">The type of the first parameter to the mapping function.</typeparam>
    /// <typeparam name="T2">The type of the second parameter to the mapping function.</typeparam>
    /// <typeparam name="T3">The type of the third parameter to the mapping function.</typeparam>
    /// <typeparam name="T4">The type of the fourth parameter to the mapping function.</typeparam>
    /// <typeparam name="T5">The type of the fifth parameter to the mapping function.</typeparam>
    /// <typeparam name="T6">The type of the sixth parameter to the mapping function.</typeparam>
    /// <typeparam name="T7">The type of the seventh parameter to the mapping function.</typeparam>
    /// <typeparam name="T8">The type of the eighth parameter to the mapping function.</typeparam>
    /// <typeparam name="T9">The type of the ninth parameter to the mapping function.</typeparam>
    /// <typeparam name="T10">The type of the tenth parameter to the mapping function.</typeparam>
    /// <returns>A task that will return the mapped objects.</returns>
    public static async Task<IReadOnlyList<TResult>> AsObjectsAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
        this Task<EagerResult<IReadOnlyList<IRecord>>> recordsTask,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> map)
    {
        var records = await recordsTask.ConfigureAwait(false);
        return records.Result.Select(r => r.AsObject(map)).ToList();
    }
}
