using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_2;
using Neo4j.Driver.Internal.Messaging.V4_2;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_2 : BoltProtocolV4_1
    {
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_2;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_2;

        public BoltProtocolV4_2(IDictionary<string, string> routingContext)
            : base(routingContext)
        {
        }

        protected override IRequestMessage HelloMessage(string userAgent,
            IDictionary<string, object> auth,
            IDictionary<string, string> routingContext)
        {
            return new HelloMessage(userAgent, auth, routingContext);
        }

        protected override IResponseHandler GetHelloResponseHandler(IConnection conn) =>
            new HelloResponseHandler(conn, Version);

    }
}
