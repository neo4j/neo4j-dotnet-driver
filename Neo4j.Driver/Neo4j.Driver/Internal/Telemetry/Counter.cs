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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Telemetry;

internal class Counter<T>
{
    // maintain a dictionary of counters, providing 0 as a default value
    // if that key has not been set yet
    private readonly ConcurrentDictionary<T, int> _counterValues = new();

    public int this[T key]
    {
        get => _counterValues.TryGetValue(key, out var value) ? value : 0;
        set => _counterValues[key] = value;
    }

    public IReadOnlyDictionary<T, int> CounterValues => _counterValues;

    public int Count => _counterValues.Count;

    public void Clear()
    {
        _counterValues.Clear();
    }
}
