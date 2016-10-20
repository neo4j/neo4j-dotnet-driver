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
    internal class RoutingDriver : IDriver
    {
        
        private ILogger _logger;
        private ILoadBalancer _loadBalancer;

        internal RoutingDriver(
            Uri seedServer, 
            IAuthToken authToken, 
            EncryptionManager encryptionManager,
            ConnectionPoolSettings poolSettings, 
            ILogger logger)
        {
            Uri = seedServer;
            _logger = logger;

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Uri Uri { get; }
        public ISession Session()
        {
            return Session(AccessMode.Write);
        }

        public ISession Session(AccessMode mode)
        {
            IPooledConnection connection = _loadBalancer.AcquireConnection(mode);

            throw new NotImplementedException();
            //return new RoutingSession(connection, mode, )
        }
    }
}