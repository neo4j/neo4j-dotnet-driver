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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal interface IPooledConnectionFactory
    {
        IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager, IConnectionListener metricsListener);
    }
    internal class PooledConnectionFactory : IPooledConnectionFactory
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly BufferSettings _bufferSettings;
        private readonly IDriverLogger _logger;

        public PooledConnectionFactory(ConnectionSettings connectionSettings, BufferSettings bufferSettings, IDriverLogger logger)
        {
            Throw.ArgumentNullException.IfNull(connectionSettings, nameof(connectionSettings));
            Throw.ArgumentNullException.IfNull(bufferSettings, nameof(bufferSettings));
            _connectionSettings = connectionSettings;
            _bufferSettings = bufferSettings;
            _logger = logger;
        }

        public IPooledConnection Create(Uri uri, IConnectionReleaseManager releaseManager, IConnectionListener metricsListener)
        {
            Throw.ArgumentNullException.IfNull(releaseManager, nameof(releaseManager));
            return new PooledConnection(new SocketConnection(uri, _connectionSettings, _bufferSettings, metricsListener, _logger),
                releaseManager, metricsListener);
        }
    }
}
