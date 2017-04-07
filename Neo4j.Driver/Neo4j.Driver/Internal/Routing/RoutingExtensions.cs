using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Routing
{
    internal static class RoutingExtensions
    {
        public static ISet<Uri> ResolveDns(this Uri uri)
        {
            return new HashSet<Uri> { uri };
        }
    }
}
