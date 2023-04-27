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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview;

namespace Neo4j.Driver.FluentQueries;

internal class ReducedExecutableQuery<TSource, TAccumulate, TResult> : IReducedExecutableQuery<TResult>
{
    private readonly Query _query;
    private readonly IQueryRowSource<TSource> _rowSource;
    private readonly QueryConfig _queryConfig;
    private readonly Func<TAccumulate> _seed;
    private readonly Func<TAccumulate, TSource, TAccumulate> _accumulate;
    private readonly Func<TAccumulate, TResult> _selectResult;

    internal ReducedExecutableQuery(
        IQueryRowSource<TSource> rowSource,
        Func<TAccumulate> seed,
        Func<TAccumulate, TSource, TAccumulate> accumulate,
        Func<TAccumulate, TResult> selectResult)
    {
        _rowSource = rowSource;
        _seed = seed;
        _accumulate = accumulate;
        _selectResult = selectResult;
    }

    public async Task<EagerResult<TResult>> ExecuteAsync(CancellationToken token = default)
    {
        var accumulator = _seed();
        var executionSummary = await _rowSource.GetRowsAsync(
            item => accumulator = _accumulate(accumulator, item),
            token);

        return new EagerResult<TResult>(_selectResult(accumulator), executionSummary.Summary, executionSummary.Keys);
    }
}
