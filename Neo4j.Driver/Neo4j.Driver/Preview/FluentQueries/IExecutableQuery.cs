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

namespace Neo4j.Driver.Experimental.FluentQueries;

/// <summary>There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.</summary>
public interface IExecutableQuery<T>
{
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Adds the specified config to the executable query.
    /// </summary>
    /// <param name="config">The query config to use.</param>
    /// <returns>The executable query object allowing method chaining.</returns>
    IExecutableQuery<T> WithConfig(QueryConfig config);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Sets the named parameters on the query.
    /// </summary>
    /// <param name="parameters">The query parameters, specified as an object which is then converted into key-value pairs.</param>
    /// <returns>The executable query object allowing method chaining.</returns>
    IExecutableQuery<T> WithParameters(object parameters);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Sets the named parameters on the query.
    /// </summary>
    /// <param name="parameters">
    /// The query's parameters, whose values should not be changed while the query is used in a
    /// session/transaction.
    /// </param>
    /// <returns>The executable query object allowing method chaining.</returns>
    IExecutableQuery<T> WithParameters(Dictionary<string, object> parameters);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// Adds a stream processor function that will be called, passing the <see cref="IAsyncEnumerable{IRecord}"/> of records
    /// returned from the query. The value returned from this callback property will be present in the
    /// <see cref="EagerResult{T}.Result"/> property of the result returned from executing the query.
    /// </summary>
    /// <param name="streamProcessor">The stream processor function.</param>
    /// <returns>The executable query object allowing method chaining.</returns>
    IExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<IRecord>, ValueTask<TResult>> streamProcessor);

    /// <summary>Executes the query as configured and returns the results, fully materialised.</summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An <see cref="EagerResult&lt;IRecord&gt;"/> containing the results of the query.</returns>
    Task<EagerResult<T>> ExecuteAsync(CancellationToken cancellationToken = default);
}
