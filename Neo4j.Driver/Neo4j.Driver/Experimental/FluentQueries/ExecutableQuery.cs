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
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Experimental.FluentQueries;

internal class ExecutableQuery<T> : IExecutableQuery<T>
{
    private readonly IInternalDriver _driver;
    private Query _query;
    private QueryConfig _queryConfig;
    private readonly Func<IAsyncEnumerable<IRecord>, ValueTask<T>> _streamProcessor;

    private ExecutableQuery(
        Query query,
        IInternalDriver driver,
        QueryConfig queryConfig,
        Func<IAsyncEnumerable<IRecord>, ValueTask<T>> streamProcessor)
    {
        _query = query;
        _driver = driver;
        _queryConfig = queryConfig;
        _streamProcessor = streamProcessor;
    }

    public IExecutableQuery<T> WithConfig(QueryConfig config)
    {
        _queryConfig = config;
        return this;
    }

    public IExecutableQuery<T> WithParameters(object parameters)
    {
        _query = new Query(_query.Text, parameters);
        return this;
    }

    public IExecutableQuery<T> WithParameters(Dictionary<string, object> parameters)
    {
        _query = new Query(_query.Text, parameters);
        return this;
    }

    public IExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<IRecord>, ValueTask<TResult>> streamProcessor)
    {
        return new ExecutableQuery<TResult>(_query, _driver, _queryConfig, streamProcessor);
    }

    // removing since behaviour is different to WithParameters, pending discussion
    // public IExecutableQuery WithParameter(string name, object value)
    // {
    //     _query.Parameters[name] = value;
    //     return this;
    // }

    public Task<EagerResult<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _driver.ExecuteQueryAsync(
            _query,
            _streamProcessor,
            _queryConfig,
            cancellationToken);
    }

    public static ExecutableQuery<IReadOnlyList<IRecord>> GetDefault(IInternalDriver driver, string cypher)
    {
        return new ExecutableQuery<IReadOnlyList<IRecord>>(new Query(cypher), driver, null, ToListAsync);
    }

    private static async ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult>(IAsyncEnumerable<TResult> enumerable)
    {
        var result = new List<TResult>();
        await foreach (var item in enumerable)
        {
            result.Add(item);
        }

        return result;
    }
}
