// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Connector.Resolvers;

internal class DefaultHostResolver : IHostResolver
{
    private static readonly bool OnMono = Type.GetType("Mono.Runtime") != null;
    private readonly IComparer<IPAddress> _addressComparer;
    private readonly IHostResolver _resolver;

    public DefaultHostResolver()
        : this(new SystemHostResolver())
    {
    }


    public DefaultHostResolver(IHostResolver resolver)
    {
        _resolver = resolver;
        _addressComparer =
            new AddressComparer(AddressFamily.InterNetworkV6 );
    }

    public IPAddress[] Resolve(string hostname)
    {
        if (TryParseIpAddress(hostname, out var result) == false)
        {
            result = _resolver.Resolve(hostname);
        }

        return result;
    }

    public async Task<IPAddress[]> ResolveAsync(string hostname)
    {
        if (TryParseIpAddress(hostname, out var result) == false)
        {
            result = await _resolver.ResolveAsync(hostname).ConfigureAwait(false);
        }

        return result;
    }

    private bool TryParseIpAddress(string hostname, out IPAddress[] resolvedAddresses)
    {
        if (IPAddress.TryParse(TranslateToMonoSafeHost(hostname), out var address))
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
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

            if (y.AddressFamily == _preferred)
            {
                return 1;
            }

            return 0;
        }
    }
}
