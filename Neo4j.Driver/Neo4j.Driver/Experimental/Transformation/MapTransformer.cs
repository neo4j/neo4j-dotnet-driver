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
using System.Threading.Tasks;

namespace Neo4j.Driver.Experimental;

internal class MapTransformer<T> : IResultTransformer<IReadOnlyList<T>>
{
    private readonly Func<IRecord, T> _transformFunc;
    private List<T> _list;

    public static Func<MapTransformer<TItem>> GetFactoryMethod<TItem>(Func<IRecord, TItem> transformFunc)
    {
        return () => new MapTransformer<TItem>(transformFunc);
    }

    public MapTransformer(Func<IRecord, T> transformFunc)
    {
        _transformFunc = transformFunc;
        _list = new();
    }

    public Task OnRecordAsync(IRecord record)
    {
        _list.Add(_transformFunc(record));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> OnFinishAsync(IResultSummary resultSummary)
    {
        return Task.FromResult((IReadOnlyList<T>)_list);
    }
}
