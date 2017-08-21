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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal static class NetworkExtensions
    {
        public static ISet<Uri> Resolve(this Uri uri)
        {
            return new HashSet<Uri> { uri };
        }

        public static async Task<IPAddress[]> ResolveAsync(this Uri uri, bool ipv6Enabled)
        {
            IPAddress[] addresses;
            IPAddress address;

            if (isLocalhost(uri) && IsRunningOnMono())
            {
                // to work around this issue: https://bugzilla.xamarin.com/show_bug.cgi?id=23862
                uri = new UriBuilder(uri.Scheme, IPAddress.Loopback.ToString(), uri.Port).Uri;
            }

            if (IPAddress.TryParse(uri.Host, out address))
            {
                if (ipv6Enabled && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // if it is a ipv4 address, then add the ipv6 address as the first attempt
                    var ipv6Address = IPAddress.IsLoopback(address) ? IPAddress.IPv6Loopback : address.MapToIPv6();
                    addresses = new[] { ipv6Address, address };
                }
                else
                {
                    addresses = new[] { address };
                }
            }
            else
            {
                var prefered = ipv6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                addresses = (await Dns.GetHostAddressesAsync(uri.Host).ConfigureAwait(false))
                    .OrderBy(x=>x, new AddressComparer(prefered)).ToArray();
            }
            return addresses;
        }

        private static bool isLocalhost(Uri uri)
        {
            return uri.Host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        internal class AddressComparer : IComparer<IPAddress>
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

        public static Uri ParseUri(this Uri uri, int defaultPort)
        {
            var port = defaultPort;
            if (uri.Port != -1)
            {
                port = uri.Port;
            }
            var builder = new UriBuilder(uri.Scheme, uri.Host, port);
            return builder.Uri;
        }

        public static IDictionary<string, string> ParseRoutingContext(this Uri uri)
        {
            string query = uri.Query;
            if (string.IsNullOrEmpty(query))
            {
                return new Dictionary<string, string>();
            }

            IDictionary<string, string> context = new Dictionary<string, string>();
            foreach (var pair in query.Split(new[] {'?', '&'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split(new []{'='}, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2)
                {
                    throw new ArgumentException(
                        $"Invalid parameters: '{pair}' in URI '{uri}'.", pair);
                }

                var key = keyValue[0];
                var value = keyValue[1];
                if (context.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"Duplicated query parameters with key '{key}' in URI '{uri}'.", key);
                }
                context.Add(key, value);
            }
            return context;
        }
    }
}
