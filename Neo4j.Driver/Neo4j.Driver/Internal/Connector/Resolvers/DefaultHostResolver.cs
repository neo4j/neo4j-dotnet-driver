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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Connector
{
    internal class DefaultHostResolver: IHostResolver
    {
        private static readonly bool OnMono = Type.GetType ("Mono.Runtime") != null;
        private readonly IHostResolver _resolver;
        private readonly bool _ipv6Preferred;
        private readonly IComparer<IPAddress> _addressComparer;

        public DefaultHostResolver(bool ipv6Preferred)
            : this(new SystemHostResolver(), ipv6Preferred)
        {

        }

        public DefaultHostResolver(IHostResolver resolver, bool ipv6Preferred)
        {
            _resolver = resolver;
            _ipv6Preferred = ipv6Preferred;
            _addressComparer =
                new AddressComparer(_ipv6Preferred ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);
        }
        
        public IPAddress[] Resolve(string hostname)
        {
            if (TryParseIpAddress(hostname, out var result) == false)
            {
                result = _resolver.Resolve(hostname);
                result = result.OrderBy(x => x, _addressComparer).ToArray();
            }

            return result;
        }

        public async Task<IPAddress[]> ResolveAsync(string hostname)
        {
            if (TryParseIpAddress(hostname, out var result) == false)
            {
                result = await _resolver.ResolveAsync(hostname).ConfigureAwait(false);
                result = result.OrderBy(x => x, _addressComparer).ToArray();
            }

            return result;
        }
        
        private bool TryParseIpAddress(string hostname, out IPAddress[] resolvedAddresses)
        {
            if (IPAddress.TryParse(TranslateToMonoSafeHost(hostname), out var address))
            {
                if (_ipv6Preferred && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    resolvedAddresses = new[]
                    {
                        IPAddress.IsLoopback(address) ? IPAddress.IPv6Loopback : address.MapToIPv6(),
                        address
                    };
                }
                else
                {
                    resolvedAddresses = new[]
                    {
                        address
                    };
                }

                return true;
            }

            resolvedAddresses = null;

            return false;
        }
                
        private static string TranslateToMonoSafeHost(string hostname) 
        { 
            if (OnMono && IsLocalhost(hostname)) 
            { 
                return IPAddress.Loopback.ToString(); 
            } 
 
            return hostname; 
        } 
 
        private static bool IsLocalhost(string hostname) 
        { 
            return hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase); 
        } 
        
        private class AddressComparer : IComparer<IPAddress> 
        { 
            private readonly AddressFamily _preferred; 
 
            public AddressComparer(AddressFamily prefered) 
            { 
                _preferred = prefered; 
            } 
 
            public int Compare(IPAddress x, IPAddress y) 
            { 
                if (x.AddressFamily == y.AddressFamily) 
                { 
                    return 0; 
                } 
                if (x.AddressFamily == _preferred) 
                { 
                    return -1; 
                } 
                else if (y.AddressFamily == _preferred) 
                { 
                    return 1; 
                } 
 
                return 0; 
            } 
        }
        
    }
}