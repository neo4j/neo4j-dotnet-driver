// Copyright (c) 2002-2017 "Neo Technology,"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Routing
{
    /// <summary>
    /// A driver with a simple load balancer to route to a cluster
    /// </summary>
    internal class RoutingDriver : BaseDriver
    {
        private ILogger _logger;
        private readonly ILoadBalancer _loadBalancer;


        internal RoutingDriver(
            ConnectionSettings connectionSettings,
            ConnectionPoolSettings poolSettings, 
            ILogger logger)
        {
            Throw.ArgumentNullException.IfNull(poolSettings, nameof(poolSettings));

            Uri = connectionSettings.InitialServerUri;
            _logger = logger;
            _loadBalancer = new RoundRobinLoadBalancer(connectionSettings, poolSettings, _logger);
        }

        public override ISession NewSession(AccessMode mode)
        {
            return new Session(_loadBalancer.AcquireConnection(mode), _logger);
        }

        public override void ReleaseUnmanagedResources()
        {

            // we cannot set loadbalancer to be null,
            // otherwise we might got NPE when concurrent calling with NewSession method
            _loadBalancer.Dispose();
 
            _logger = null;
        }

        public override Uri Uri { get; }
    }
}