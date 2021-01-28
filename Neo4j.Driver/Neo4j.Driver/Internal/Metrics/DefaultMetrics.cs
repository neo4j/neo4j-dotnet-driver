// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Neo4j.Driver.Internal.Metrics
{
    internal interface IInternalMetrics : IMetrics, IMetricsListener{}

    internal interface IMetricsListener
    {
        IConnectionPoolListener PutPoolMetrics(string poolId, IConnectionPool pool);
        void RemovePoolMetrics(string poolId);
    }

    internal class DefaultMetrics : IInternalMetrics
    {
        private readonly ConcurrentDictionary<string, IInternalConnectionPoolMetrics> _poolMetrics;

        public DefaultMetrics()
        {
            _poolMetrics = new ConcurrentDictionary<string, IInternalConnectionPoolMetrics>();
        }

        public IDictionary<string, IConnectionPoolMetrics> ConnectionPoolMetrics =>
            new ReadOnlyDictionary<string, IConnectionPoolMetrics>(
                _poolMetrics.ToDictionary(item => item.Key, item => (IConnectionPoolMetrics) item.Value));

        public IConnectionPoolListener PutPoolMetrics(string poolId, IConnectionPool pool)
        {
            var poolMetrics = new ConnectionPoolMetrics(poolId, pool, this);
            return _poolMetrics.GetOrAdd(poolId, poolMetrics);
        }

        public void RemovePoolMetrics(string poolId)
        {
            _poolMetrics.TryRemove(poolId, out _);
        }
    }
}
