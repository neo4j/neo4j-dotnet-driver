
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_2;
using Neo4j.Driver.Internal.Messaging.V4_2;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_2 : BoltProtocolV4_1
    {
        private static int _major = 4;
        private static int _minor = 2;
        public static new BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
        public override BoltProtocolVersion GetVersion() { return Version; }

		protected override IMessageFormat MessageFormat { get { return BoltProtocolMessageFormat.V4_2; } }
		protected override IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth,
														IDictionary<string, string> routingContext)
		{
			return new HelloMessage(userAgent, auth, routingContext);
		}
		protected override IResponseHandler HelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }

		protected BoltProtocolV4_2()
        {
        }

        public BoltProtocolV4_2(IDictionary<string, string> routingContext) : base(routingContext)
        {
        }
    }
}
