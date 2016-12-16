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
        private IConnectionPool _connectionPool;
        private ILogger _logger;

        internal DirectDriver(Uri uri, IAuthToken authToken, EncryptionManager encryptionManager, ConnectionPoolSettings connectionPoolSettings, ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(uri, nameof(uri));
            Throw.ArgumentNullException.IfNull(authToken, nameof(authToken));
            Throw.ArgumentNullException.IfNull(encryptionManager, nameof(encryptionManager));
            Throw.ArgumentNullException.IfNull(connectionPoolSettings, nameof(connectionPoolSettings));

            Uri = uri;
            _logger = logger;
            _connectionPool = new ConnectionPool(uri, authToken, encryptionManager, connectionPoolSettings, _logger);
        }

        public override ISession NewSession(AccessMode mode)
        {
            return new Session(_connectionPool.Acquire(), _logger);
        }

        public override void ReleaseUnmanagedResources()
        {
            if (_connectionPool != null)
            {
                _connectionPool.Dispose();
                _connectionPool = null;
            }
            _logger = null;
        }

        public override Uri Uri { get; }
    }
}