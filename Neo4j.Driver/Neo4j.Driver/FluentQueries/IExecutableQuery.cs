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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Exposes methods for configuring and executing a driver-level query.
/// </summary>
/// <typeparam name="TIn">The type of items the query will receive.</typeparam>
/// <typeparam name="TOut">The type of items the query will output.</typeparam>
public interface IExecutableQuery<TIn, TOut> : IConfiguredQuery<TIn, TOut>
{
    /// <summary>
    /// Sets the query config on the query to be executed.
    /// </summary>
    /// <param name="config">The config to set.</param>
    /// <returns>The same instance to allow method chaining.</returns>
    IExecutableQuery<TIn, TOut> WithConfig(QueryConfig config);

    /// <summary>
    /// Sets the parameters on the query to be executed.
    /// </summary>
    /// <param name="parameters">A dictionary of parameter values, keyed by their names.</param>
    /// <returns>The same instance to allow method chaining.</returns>
    IExecutableQuery<TIn, TOut> WithParameters(Dictionary<string, object> parameters);

    /// <summary>
    /// Sets the parameters on the query to be executed.
    /// </summary>
    /// <param name="parameters">An object whose properties have names matching the names of the query's parameters
    /// and values that should be used as those parameters' values.</param>
    /// <returns>The same instance to allow method chaining.</returns>
    IExecutableQuery<TIn, TOut> WithParameters(object parameters);

    /// <summary>
    /// Specifies a stream processor that will process the <see cref="IAsyncEnumerable{IRecord}"/> that results
    /// from a query, and return the value that will be given as the result of the query.
    /// </summary>
    /// <param name="streamProcessor">An asynchronous method that will process the supplied asynchronous
    /// stream of records.</param>
    /// <typeparam name="TResult">The type of the return value from <see cref="streamProcessor"/>.</typeparam>
    /// <returns>The same instance which can only be used to execute the query.</returns>
    IReducedExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<TIn>, Task<TResult>> streamProcessor);
}

/// <summary>
/// A query that can no longer be configured.
/// </summary>
/// <typeparam name="TIn">The type of items that will be input into this instance.</typeparam>
/// <typeparam name="TOut">The type of items that will be output from this instance.</typeparam>
public interface IConfiguredQuery<TIn, TOut>
{
    /// <summary>
    /// Specifies a filter that will be applied to all results in the query. If the specified delegate returns
    /// <c>true</c>, the row will be included in the results. If it returns <c>false</c>, it will be excluded.
    /// </summary>
    /// <param name="filter">The predicate that will decide whether an item is present in the results.</param>
    /// <returns>The same instance for method chaining.</returns>
    IConfiguredQuery<TIn, TOut> WithFilter(Func<TOut, bool> filter);

    /// <summary>
    /// Specifies a mapping that will be used to turn items of type <see cref="TOut"/> into items of type
    /// <see cref="TNext"/>.
    /// </summary>
    /// <param name="map">The mapping function.</param>
    /// <typeparam name="TNext">The output type of the mapping function.</typeparam>
    /// <returns>A new instance whose input type is <see cref="TOut"/> and whose input type is <see cref="TNext"/>
    /// </returns>
    IConfiguredQuery<TOut, TNext> WithMap<TNext>(Func<TOut, TNext> map);

    /// <summary>
    /// Specifies a method of reducing many items of type <see cref="TOut"/> into one instance of type
    /// <see cref="TResult"/>.
    /// </summary>
    /// <param name="seed">The initial value of the resulting value.</param>
    /// <param name="accumulate">A method that will accumulate each value of type <see cref="TOut"/> into
    /// the result value.</param>
    /// <typeparam name="TResult">The type of the reduced result.</typeparam>
    /// <returns>The same instance which can only be used to execute the query.</returns>
    IReducedExecutableQuery<TResult> WithReduce<TResult>(
        Func<TResult> seed,
        Func<TResult, TOut, TResult> accumulate);

    /// <summary>
    /// Specifies a method of reducing many items of type <see cref="TOut"/> into one instance of type
    /// <see cref="TResult"/>, using one method to accumulate the values into a value of type <see cref="TAccumulate"/>
    /// and another to turn the accumulated value into a result.
    /// </summary>
    /// <param name="seed">The initial value of the accumulating value.</param>
    /// <param name="accumulate">A method that will accumulate each value of type <see cref="TOut"/> into
    /// the accumulated value.</param>
    /// <param name="selectResult">A method that will turn the accumulated value into a value of type
    /// <see cref="TResult"/>.</param>
    /// <typeparam name="TAccumulate"></typeparam>
    /// <typeparam name="TResult">The type of the reduced result.</typeparam>
    /// <returns>The same instance which can only be used to execute the query.</returns>
    IReducedExecutableQuery<TResult> WithReduce<TAccumulate, TResult>(
        Func<TAccumulate> seed,
        Func<TAccumulate, TOut, TAccumulate> accumulate,
        Func<TAccumulate, TResult> selectResult);

    /// <summary>
    /// Executes the query.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="EagerResult{TOut}"/>That contains the result of the query and
    /// information about the execution.</returns>
    Task<EagerResult<IReadOnlyList<TOut>>> ExecuteAsync(CancellationToken token = default);
}

public interface IReducedExecutableQuery<TOut>
{
    /// <summary>
    /// Executes the query.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="EagerResult{TOut}"/>That contains the result of the query and
    /// information about the execution.</returns>
    Task<EagerResult<TOut>> ExecuteAsync(CancellationToken token = default);
}
