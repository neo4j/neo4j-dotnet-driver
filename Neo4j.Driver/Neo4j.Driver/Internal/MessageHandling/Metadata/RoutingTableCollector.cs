using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
    class RoutingTableCollector : IMetadataCollector<IDictionary<string, object>>
    {
        internal const string RoutingTableKey = "rt";
        internal const string TimeoutKey = "ttl";
        internal const string ServersKey = "servers";
        internal const string DatabaseKey = "db";

        object IMetadataCollector.Collected => Collected;

        public IDictionary<string, object> Collected { get; private set; }

        public void Collect(IDictionary<string, object> metadata)
        {
            if(metadata != null && metadata.TryGetValue(RoutingTableKey, out var routingTable))
            {
                if(routingTable is IDictionary<string, object> rt)
                {
                    Collected = rt;
                }
                else
                {
                    throw new ProtocolException($"Expected '{RoutingTableKey}' metadata to be of type 'Dictionary<string, object>', but got '{routingTable?.GetType().Name}'.");
                }
            }
        }
    }
}
