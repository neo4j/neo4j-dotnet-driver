// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class ConnectionPoolSettings
    {
        public int MaxIdleConnectionPoolSize { get; }
        public int MaxConnectionPoolSize { get; }
        public TimeSpan ConnectionAcquisitionTimeout { get; }
        public TimeSpan ConnectionIdleTimeout { get; }
        public TimeSpan MaxConnectionLifetime { get; }


        public IStatisticsCollector StatisticsCollector { get; }

        public ConnectionPoolSettings(Config config)
            :this(config.MaxIdleConnectionPoolSize, config.MaxConnectionPoolSize, config.ConnectionAcquisitionTimeout,
                 config.ConnectionIdleTimeout, config.MaxConnectionLifetime, config.DriverStatisticsCollector)
        {
        }

        internal ConnectionPoolSettings(int maxIdleConnectionPoolSize, int maxConnectionPoolSize,
            TimeSpan connectionAcquisitionTimeout, TimeSpan connectionIdleTimeout, TimeSpan maxConnectionLifetime,
            IStatisticsCollector statisticsCollector=null)
        {
            MaxIdleConnectionPoolSize = maxIdleConnectionPoolSize;
            MaxConnectionPoolSize = maxConnectionPoolSize;
            ConnectionAcquisitionTimeout = connectionAcquisitionTimeout;
            ConnectionIdleTimeout = connectionIdleTimeout;
            MaxConnectionLifetime = maxConnectionLifetime;
            StatisticsCollector = statisticsCollector;
        }
    }
}
