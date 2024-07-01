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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal interface IDriverRowSource<T> : IQueryRowSource<T>
{
    void SetConfig(QueryConfig config);
    void SetParameters(Dictionary<string, object> parameters);
    void SetParameters(object parameters);

    Task<EagerResult<TResult>> ProcessStreamAsync<TResult>(
        Func<IAsyncEnumerable<T>, Task<TResult>> streamProcessor,
        CancellationToken cancellationToken = default);
}

internal class DriverRowSource : IDriverRowSource<IRecord>
{
    private readonly IInternalDriver _driver;
    private Query _query;
    private QueryConfig _queryConfig;

    internal DriverRowSource(IInternalDriver driver, string cypher)
    {
        _driver = driver;
        _query = new Query(cypher);
    }

    public Task<ExecutionSummary> GetRowsAsync(
        Action<IRecord> rowProcessor,
        CancellationToken cancellationToken = default)
    {
        return _driver.GetRowsAsync(_query, _queryConfig, rowProcessor, cancellationToken);
    }

    public Task<EagerResult<TResult>> ProcessStreamAsync<TResult>(
        Func<IAsyncEnumerable<IRecord>, Task<TResult>> streamProcessor,
        CancellationToken cancellationToken = default)
    {
        return _driver.ExecuteQueryAsync(_query, streamProcessor, _queryConfig, cancellationToken);
    }

    public void SetConfig(QueryConfig config)
    {
        _queryConfig = config;
    }

    public void SetParameters(Dictionary<string, object> parameters)
    {
        _query = new Query(_query.Text, parameters);
    }

    public void SetParameters(object parameters)
    {
        _query = new Query(_query.Text, parameters);
    }
}
