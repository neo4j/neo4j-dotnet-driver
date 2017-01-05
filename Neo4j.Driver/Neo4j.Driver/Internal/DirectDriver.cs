// Copyright (c) 2002-2016 "Neo Technology,"
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
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class DirectDriver : BaseDriver
    {
        private readonly IConnectionPool _connectionPool;
        private ILogger _logger;

        internal DirectDriver(ConnectionSettings connectionSettings, ConnectionPoolSettings connectionPoolSettings, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(connectionSettings, nameof(connectionSettings));
            Throw.ArgumentNullException.IfNull(connectionPoolSettings, nameof(connectionPoolSettings));

            Uri = connectionSettings.InitialServerUri;
            _logger = logger;
            _connectionPool = new ConnectionPool(connectionSettings, connectionPoolSettings, _logger);
        }

        public override ISession NewSession(AccessMode mode)
        {
            return new Session(_connectionPool.Acquire(), _logger);
        }

        public override void ReleaseUnmanagedResources()
        {
            // We cannot set connection pool to be null,
            // otherwise we might get NPE when using concurrently with NewSession
            _connectionPool.Dispose();
            _logger = null;
        }

        public override Uri Uri { get; }
    }
}