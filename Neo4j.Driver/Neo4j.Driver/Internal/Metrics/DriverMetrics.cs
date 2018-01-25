// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class DriverMetrics : IDriverMetrics
    {
        public IDictionary<string, IConnectionPoolMetrics> PoolMetrics { get; }
        public IDictionary<string, IConnectionMetrics> ConnectionMetrics { get; }
        private readonly Config _config;

        public DriverMetrics(Config config)
        {
            _config = config;
            PoolMetrics = new ConcurrentDictionary<string, IConnectionPoolMetrics>();
            ConnectionMetrics = new ConcurrentDictionary<string, IConnectionMetrics>();
        }

        public ConnectionPoolMetrics AddPoolMetrics(Uri poolUri, IConnectionPool pool)
        {
            var acquisitionTimeout = _config.ConnectionAcquisitionTimeout;
            var poolMetrics = new ConnectionPoolMetrics(poolUri, pool, acquisitionTimeout);
            PoolMetrics.Add(poolMetrics.UniqueName, poolMetrics);

            return poolMetrics;
        }

        public ConnectionMetrics AddConnMetrics(Uri poolUri)
        {
            var connectionTimeout = _config.ConnectionTimeout;
            var connMetrics = new ConnectionMetrics(poolUri, connectionTimeout);
            ConnectionMetrics.Add(connMetrics.UniqueName, connMetrics);

            return connMetrics;
        }
    }
}
