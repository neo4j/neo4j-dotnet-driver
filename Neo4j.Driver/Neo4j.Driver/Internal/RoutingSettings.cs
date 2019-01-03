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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Routing;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal
{
    internal class RoutingSettings
    {
        public IDictionary<string, string> RoutingContext { get; }
        public InitialServerAddressProvider InitialServerAddressProvider { get; }
        public LoadBalancingStrategy Strategy { get; }

        public RoutingSettings(Uri initServerUri, IDictionary<string, string> routingContext, Config config)
        {
            Throw.ArgumentNullException.IfNull(initServerUri, nameof(initServerUri));
            Throw.ArgumentNullException.IfNull(routingContext, nameof(routingContext));
            Throw.ArgumentNullException.IfNull(config, nameof(config));

            InitialServerAddressProvider = new InitialServerAddressProvider(initServerUri, config.Resolver);
            RoutingContext = routingContext;
            Strategy = config.LoadBalancingStrategy;
        }
    }

    internal interface IInitialServerAddressProvider
    {
        ISet<Uri> Get();
    }

    internal class InitialServerAddressProvider : IInitialServerAddressProvider
    {
        private readonly Uri _initAddress;
        private readonly IServerAddressResolver _resolver;
        public InitialServerAddressProvider(Uri initialServerAddress, IServerAddressResolver resolver)
        {
            _initAddress = initialServerAddress;
            _resolver = resolver;
        }

        public ISet<Uri> Get()
        {
            var set = new HashSet<Uri>();
            var addresses = _resolver.Resolve(ServerAddress.From(_initAddress));
            foreach (var address in addresses)
            {
                // for now we convert this ServerAddress back to Uri
                set.Add(new UriBuilder("bolt+routing://", address.Host, address.Port).Uri);
            }
            return set;
        }
    }
}
