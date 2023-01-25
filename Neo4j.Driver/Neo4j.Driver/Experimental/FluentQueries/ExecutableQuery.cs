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

internal class ExecutableQuery : IExecutableQuery
{
    private IInternalDriver _driver;
    private Query _query;
    private QueryConfig _queryConfig;

    public ExecutableQuery(IInternalDriver driver, string cypher)
    {
        _driver = driver;
        _query = new Query(cypher, new {});
    }

    public IExecutableQuery WithConfig(QueryConfig config)
    {
        _queryConfig = config;
        return this;
    }

    public IExecutableQuery WithParameters(object parameters)
    {
        _query = new Query(_query.Text, parameters);
        return this;
    }

    public IExecutableQuery WithParameters(Dictionary<string, object> parameters)
    {
        _query = new Query(_query.Text, parameters);
        return this;
    }

    public IExecutableQuery WithParameter(string name, object value)
    {
        _query.Parameters[name] = value;
        return this;
    }

    public Task<EagerResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _driver.ExecuteQueryAsync(_query, _queryConfig, cancellationToken);
    }

    private Task<IReadOnlyList<T>> TransformRecordsAsync<T>(
        Func<IRecord, T> transform,
        CancellationToken cancellationToken = default)
    {
        return _driver.ExecuteQueryAsync(_query, MapTransformer<T>.GetFactoryMethod(transform), _queryConfig, cancellationToken);
    }
}
