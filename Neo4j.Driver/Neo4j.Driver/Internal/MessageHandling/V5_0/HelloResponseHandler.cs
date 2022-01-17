using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V5_0
{
    internal class HelloResponseHandler : V4_4.HelloResponseHandler
    {
        public HelloResponseHandler(IConnection connection, BoltProtocolVersion version) : base(connection, version)
        {
        }
    }
}
