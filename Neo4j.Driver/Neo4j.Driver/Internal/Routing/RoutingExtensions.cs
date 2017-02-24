using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Routing
{
    internal static class RoutingExtensions
    {
        public static ISet<Uri> ToIps(this Uri uri)
        {
            return new HashSet<Uri> { uri };
        }
    }
}
