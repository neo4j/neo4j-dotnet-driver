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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview;

namespace Neo4j.Driver.FluentQueries;

internal class ExecutableQuery<TIn, TOut> : IExecutableQuery<TIn, TOut>, IQueryRowSource<TOut>
{
    private readonly IQueryRowSource<TIn> _rowSource;
    private readonly Func<TIn, TOut> _mapper;
    private readonly List<Func<TOut, bool>> _filters = new();
    private Func<TOut> _reduceSeed;
    private Action<TOut, TIn, TOut> _accumulateValue;

    internal ExecutableQuery(
        IQueryRowSource<TIn> rowSource,
        Func<TIn, TOut> mapper)
    {
        _rowSource = rowSource;
        _mapper = mapper;
    }

    public IExecutableQuery<TIn, TOut> WithConfig(QueryConfig config)
    {
        if (_rowSource is IDriverRowSource<TIn> driverRowSource)
        {
            driverRowSource.SetConfig(config);
        }

        return this;
    }

    public IExecutableQuery<TIn, TOut> WithParameters(object parameters)
    {
        if (_rowSource is IDriverRowSource<TIn> driverRowSource)
        {
            driverRowSource.SetParameters(parameters);
        }

        return this;
    }

    public IExecutableQuery<TIn, TOut> WithParameters(Dictionary<string, object> parameters)
    {
        if (_rowSource is IDriverRowSource<TIn> driverRowSource)
        {
            driverRowSource.SetParameters(parameters);
        }

        return this;
    }

    public IStreamProcessorExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<TIn>, Task<TResult>> streamProcessor)
    {
        if (_rowSource is IDriverRowSource<TIn> driverRowSource)
        {
            return new StreamProcessorExecutableQuery<TIn, TResult>(driverRowSource, streamProcessor);
        }

        // this can't actually happen, throwing to satisfy the compiler and for safety
        throw new InvalidOperationException("WithStreamProcessor cannot be called on nested queries");
    }

    public IConfiguredQuery<TIn, TOut> WithFilter(Func<TOut, bool> filter)
    {
        _filters.Add(filter);
        return this;
    }

    public IConfiguredQuery<TOut, TNext> WithMap<TNext>(
        Func<TOut, TNext> map)
    {
        return new ExecutableQuery<TOut, TNext>(this, map);
    }

    public IReducedExecutableQuery<TResult> WithReduce<TAccumulate, TResult>(
        Func<TAccumulate> seed,
        Func<TAccumulate, TOut, TAccumulate> accumulate,
        Func<TAccumulate, TResult> selectResult)
    {
        return new ReducedExecutableQuery<TOut, TAccumulate, TResult>(this, seed, accumulate, selectResult);
    }

    public IReducedExecutableQuery<TResult> WithReduce<TResult>(
        Func<TResult> seed,
        Func<TResult, TOut, TResult> accumulate)
    {
        return new ReducedExecutableQuery<TOut, TResult, TResult>(this, seed, accumulate, x => x);
    }

    public Task<EagerResult<IReadOnlyList<TOut>>> ExecuteAsync(CancellationToken token = default)
    {
        return WithReduce(ReduceToList<TOut>.Seed, ReduceToList<TOut>.Accumulate, ReduceToList<TOut>.SelectResult)
            .ExecuteAsync(token);
    }

    public Task<ExecutionSummary> GetRowsAsync(
        Action<TOut> rowProcessor,
        CancellationToken cancellationToken = default)
    {
        void ProcessRow(TIn rowItem)
        {
            var mapped = _mapper(rowItem);
            if (_filters.All(f => f(mapped)))
            {
                rowProcessor(mapped);
            }
        }

        return _rowSource.GetRowsAsync(ProcessRow, cancellationToken);
    }
}
