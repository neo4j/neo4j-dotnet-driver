using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4j.Driver
{
    public static class GraphDatabase
    {
        public static Driver Driver(string uri)
        {
            return Driver(new Uri(uri));
        }


        public static Driver Driver(Uri url)
        {
            return Driver(url, Config.DefaultConfig());
        }


        public static Driver Driver(Uri url, Config config)
        {
            return new Driver(url, config);
        }

        public static Driver Driver(String uri, Config config)
        {
            return Driver(new Uri(uri), config);
        }
    }
}
