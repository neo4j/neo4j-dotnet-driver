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

        public static async Task<IPAddress[]> ResolveAsyc(this Uri uri)
        {
            IPAddress[] addresses;
            IPAddress address;
            if (IPAddress.TryParse(uri.Host, out address))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // if it is a ipv4 address, then add the ipv6 address as the first attempt
                    addresses = new[] { address.MapToIPv6(), address };
                }
                else
                {
                    addresses = new[] { address };
                }
            }
            else
            {
                addresses = (await Dns.GetHostAddressesAsync(uri.Host).ConfigureAwait(false))
                    .OrderBy(x=>x, new AddressComparer(AddressFamily.InterNetworkV6)).ToArray();
            }
            return addresses;
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
    }
}
