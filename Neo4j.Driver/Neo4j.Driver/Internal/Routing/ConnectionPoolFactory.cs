// Copyright (c) 2002-2019 "Neo4j,"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    internal interface IConnectionPoolFactory
    {
        IConnectionPool Create(Uri uri);
    }

    internal class ConnectionPoolFactory : IConnectionPoolFactory
    {
        private readonly IPooledConnectionFactory _connectionFactory;
        private readonly ConnectionPoolSettings _poolSettings;
        private readonly IDriverLogger _logger;

        public ConnectionPoolFactory(IPooledConnectionFactory connectionFactory, ConnectionPoolSettings poolSettings,
            IDriverLogger logger)
        {
            Throw.ArgumentNullException.IfNull(connectionFactory, nameof(connectionFactory));
            Throw.ArgumentNullException.IfNull(poolSettings, nameof(poolSettings));
            _connectionFactory = connectionFactory;
            _poolSettings = poolSettings;
            _logger = logger;
        }

        public IConnectionPool Create(Uri uri)
        {
            return new ConnectionPool(uri, _connectionFactory, _poolSettings, _logger);
        }
    }
}
