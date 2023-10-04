// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing;

internal interface IConnectionPoolFactory
{
    IConnectionPool Create(Uri uri);
}

internal sealed class ConnectionPoolFactory : IConnectionPoolFactory
{
    private readonly IPooledConnectionFactory _connectionFactory;
    private readonly DriverContext _driverContext;
    private readonly ILogger _logger;
    private readonly IDictionary<string, string> _routingContext;

    public ConnectionPoolFactory(
        IPooledConnectionFactory connectionFactory,
        IDictionary<string, string> routingContext,
        DriverContext driverContext,
        ILogger logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _routingContext = routingContext;
        _driverContext = driverContext;
    }

    public IConnectionPool Create(Uri uri)
    {
        return new ConnectionPool(
            uri,
            _connectionFactory,
            _logger,
            _driverContext,
            _routingContext);
    }
}
