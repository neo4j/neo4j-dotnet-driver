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

namespace Neo4j.Driver.Internal.Metrics
{
    internal interface IDriverMetricsManager : IDisposable
    {
        IConnectionPoolListener PoolMetricsListener { get; }
        IConnectionListener ConnectionMetricsListener { get; }
    }

    internal class DevNullDriverMetricsManager : IDriverMetricsManager
    {
        public void Dispose()
        {
            // Left empty on purpose
        }

        public IConnectionPoolListener PoolMetricsListener => null;
        public IConnectionListener ConnectionMetricsListener => null;
    }

    internal class DriverMetricsManager : IDriverMetricsManager
    {
        private readonly ConnectionPoolMetrics _poolMetrics;
        private readonly ConnectionMetrics _connMetrics;

        public IConnectionPoolListener PoolMetricsListener => _poolMetrics;
        public IConnectionListener ConnectionMetricsListener => _connMetrics;

        public DriverMetricsManager(Metrics metrics, Uri poolUri, ConnectionPool pool)
        {
            Throw.ArgumentNullException.IfNull(metrics, nameof(metrics));
            Throw.ArgumentNullException.IfNull(metrics.ConnectionMetrics, nameof(metrics.ConnectionMetrics));
            Throw.ArgumentNullException.IfNull(metrics.ConnectionPoolMetrics, nameof(metrics.ConnectionPoolMetrics));
            Throw.ArgumentNullException.IfNull(poolUri, nameof(poolUri));
            Throw.ArgumentNullException.IfNull(pool, nameof(pool));

            _poolMetrics = metrics.AddPoolMetrics(poolUri, pool);
            _connMetrics = metrics.AddConnMetrics(poolUri);
        }

        public void Dispose()
        {
            _poolMetrics.Dispose();
        }
    }
}
