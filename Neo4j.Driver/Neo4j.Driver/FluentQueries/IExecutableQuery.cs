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
using Neo4j.Driver.Preview;

namespace Neo4j.Driver.FluentQueries;

public interface IExecutableQuery<TIn, TOut> : IConfiguredQuery<TIn, TOut>
{
    IExecutableQuery<TIn, TOut> WithConfig(QueryConfig config);
    IExecutableQuery<TIn, TOut> WithParameters(Dictionary<string, object> parameters);
    IExecutableQuery<TIn, TOut> WithParameters(object parameters);

    IStreamProcessorExecutableQuery<TResult> WithStreamProcessor<TResult>(
        Func<IAsyncEnumerable<TIn>, Task<TResult>> streamProcessor);
}

public interface IConfiguredQuery<TIn, TOut>
{
    IConfiguredQuery<TIn, TOut> WithFilter(Func<TOut, bool> filter);
    IConfiguredQuery<TOut, TNext> WithMap<TNext>(Func<TOut, TNext> map);

    IReducedExecutableQuery<TResult> WithReduce<TResult>(
        Func<TResult> seed,
        Func<TResult, TOut, TResult> accumulate);

    IReducedExecutableQuery<TResult> WithReduce<TAccumulate, TResult>(
        Func<TAccumulate> seed,
        Func<TAccumulate, TOut, TAccumulate> accumulate,
        Func<TAccumulate, TResult> selectResult);

    Task<EagerResult<IReadOnlyList<TOut>>> ExecuteAsync(CancellationToken token = default);
}

public interface IReducedExecutableQuery<TOut>
{
    Task<EagerResult<TOut>> ExecuteAsync(CancellationToken token = default);
}
