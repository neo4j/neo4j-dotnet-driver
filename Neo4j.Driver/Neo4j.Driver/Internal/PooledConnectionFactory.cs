// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal;

internal interface IPooledConnectionFactory
{
    IPooledConnection Create(
        Uri uri,
        IConnectionReleaseManager releaseManager,
        IDictionary<string, string> routingContext);
}

internal class PooledConnectionFactory : IPooledConnectionFactory
{
    private readonly BufferSettings _bufferSettings;
    private readonly ConnectionSettings _connectionSettings;
    private readonly ILogger _logger;

    public PooledConnectionFactory(ConnectionSettings connectionSettings, BufferSettings bufferSettings, ILogger logger)
    {
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _bufferSettings = bufferSettings ?? throw new ArgumentNullException(nameof(bufferSettings));
        _logger = logger;
    }

    public IPooledConnection Create(
        Uri uri,
        IConnectionReleaseManager releaseManager,
        IDictionary<string, string> routingContext)
    {
        return new PooledConnection(
            new SocketConnection(uri, _connectionSettings, _bufferSettings, routingContext, _logger),
            releaseManager ?? throw new ArgumentNullException(nameof(releaseManager)));
    }
}
