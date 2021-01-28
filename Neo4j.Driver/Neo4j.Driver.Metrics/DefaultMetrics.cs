﻿// Copyright (c) "Neo4j"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Metrics
{
    internal class DefaultMetrics : IMetrics
    {
        private readonly ConcurrentDictionary<string, IConnectionPoolMetrics> _poolMetrics;
        private readonly ConcurrentDictionary<string, IConnectionMetrics> _connMetrics;
        private readonly Config _config;

        public DefaultMetrics(Config config)
        {
            _config = config;
            _poolMetrics = new ConcurrentDictionary<string, IConnectionPoolMetrics>();
            _connMetrics = new ConcurrentDictionary<string, IConnectionMetrics>();
        }

        public IConnectionPoolListener CreateConnectionPoolListener(Uri poolUri, IConnectionPool pool)
        {
            var acquisitionTimeout = _config.ConnectionAcquisitionTimeout;
            var poolMetrics = new ConnectionPoolMetrics(poolUri, pool, acquisitionTimeout);
            var key = poolMetrics.UniqueName;

            return (IConnectionPoolListener) _poolMetrics.GetOrAdd(key, poolMetrics);
        }

        public IConnectionListener CreateConnectionListener(Uri poolUri)
        {
            var connectionTimeout = _config.ConnectionTimeout;
            var connMetrics = new ConnectionMetrics(poolUri, connectionTimeout);
            var key = connMetrics.UniqueName;

            return (IConnectionListener) _connMetrics.GetOrAdd(key, connMetrics);
        }

        public IDictionary<string, IConnectionPoolMetrics> ConnectionPoolMetrics => new ReadOnlyDictionary<string, IConnectionPoolMetrics>(_poolMetrics);
        public IDictionary<string, IConnectionMetrics> ConnectionMetrics => new ReadOnlyDictionary<string, IConnectionMetrics>(_connMetrics);
    }
}
