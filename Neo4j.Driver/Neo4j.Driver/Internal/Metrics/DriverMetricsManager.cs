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
        private readonly IDriverMetrics _driverMetrics;
        private ConnectionPoolMetrics _poolMetrics;
        private ConnectionMetrics _connMetrics;

        public IConnectionPoolListener PoolMetricsListener => _poolMetrics;
        public IConnectionListener ConnectionMetricsListener => _connMetrics;

        public DriverMetricsManager(IDriverMetrics driverMetrics, Uri poolUri, ConnectionPool pool, TimeSpan acquisitionTimeout, TimeSpan connectionTimeout)
        {
            _driverMetrics = driverMetrics;
            RegisterAtDriverMetrics(poolUri, pool, acquisitionTimeout, connectionTimeout);
        }

        public void Dispose()
        {
            UnregisterFromDriverMetrics();
        }

        private void RegisterAtDriverMetrics(Uri poolUri, ConnectionPool pool, TimeSpan acquisitionTimeout, TimeSpan connectionTimeout)
        {
            if (_driverMetrics?.PoolMetrics != null)
            {
                _poolMetrics = new ConnectionPoolMetrics(poolUri, pool, acquisitionTimeout);
                _driverMetrics.PoolMetrics.Add(_poolMetrics.UniqueName, _poolMetrics);
            }
            if (_driverMetrics?.ConnectionMetrics != null)
            {
                _connMetrics = new ConnectionMetrics(poolUri, connectionTimeout);
                _driverMetrics.ConnectionMetrics.Add(_connMetrics.UniqueName, _connMetrics);
            }
        }

        private void UnregisterFromDriverMetrics()
        {
            if (_poolMetrics != null)
            {
                _driverMetrics?.PoolMetrics?.Remove(_poolMetrics.UniqueName);
                _poolMetrics.Dispose();
            }
            if (_connMetrics != null)
            {
                _driverMetrics?.ConnectionMetrics?.Remove(_connMetrics.UniqueName);
            }
        }

    }
}
