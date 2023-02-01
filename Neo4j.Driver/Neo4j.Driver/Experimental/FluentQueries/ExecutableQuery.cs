// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
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
    private IInternalDriver _driver;
    private Func<IRecord, T> _transformFunc;
    private Query _query;
    private QueryConfig _queryConfig;

    public ExecutableQuery(IInternalDriver driver, string cypher)
    {
        _driver = driver;
        _transformFunc = r => (T)r;
        _query = new Query(cypher, new {});
    }

    private ExecutableQuery(
        Query query,
        IInternalDriver driver,
        QueryConfig queryConfig,
        Func<IRecord, T> transformFunc)
    {
        _query = query;
        _driver = driver;
        _queryConfig = queryConfig;
        _transformFunc = transformFunc;
    }

    private static ExecutableQuery<TOut> Create<TIn, TOut>(
        IExecutableQuery<TIn> executableQuery,
        Func<TIn, TOut> transform)
    {
        var other = (ExecutableQuery<TIn>)executableQuery;

        return new ExecutableQuery<TOut>(
            other._query,
            other._driver,
            other._queryConfig,
            record =>
            {
                var inValue = other._transformFunc(record);
                return transform(inValue);
            });
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

    public IExecutableQuery<TResult> WithTransformation<TResult>(Func<T, TResult> transform)
    {
        return Create(this, transform);
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
            MapTransformer<T>.GetFactoryMethod(_transformFunc),
            _queryConfig,
            cancellationToken);
    }
}
