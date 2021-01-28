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
using Neo4j.Driver.Internal.Metrics;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPoolSettings
    {
        public int MaxIdleConnectionPoolSize { get; }
        public int MaxConnectionPoolSize { get; }
        public TimeSpan ConnectionAcquisitionTimeout { get; }
        public TimeSpan ConnectionIdleTimeout { get; }
        public TimeSpan MaxConnectionLifetime { get; }
        public IInternalMetrics Metrics { get; }

        public ConnectionPoolSettings(Config config, IInternalMetrics metrics = null)
            : this(config.MaxIdleConnectionPoolSize, config.MaxConnectionPoolSize, config.ConnectionAcquisitionTimeout,
                config.ConnectionIdleTimeout, config.MaxConnectionLifetime, metrics)
        {
        }

        internal ConnectionPoolSettings(int maxIdleConnectionPoolSize, int maxConnectionPoolSize,
            TimeSpan connectionAcquisitionTimeout, TimeSpan connectionIdleTimeout, TimeSpan maxConnectionLifetime,
            IInternalMetrics metrics = null)
        {
            MaxIdleConnectionPoolSize = maxIdleConnectionPoolSize;
            MaxConnectionPoolSize = maxConnectionPoolSize;
            ConnectionAcquisitionTimeout = connectionAcquisitionTimeout;
            ConnectionIdleTimeout = connectionIdleTimeout;
            MaxConnectionLifetime = maxConnectionLifetime;
            Metrics = metrics;
        }
    }
}
