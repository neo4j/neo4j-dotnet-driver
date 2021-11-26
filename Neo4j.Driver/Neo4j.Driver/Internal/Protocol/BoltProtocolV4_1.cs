
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_1;
using Neo4j.Driver.Internal.Messaging.V4_1;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_1 : BoltProtocolV4_0
    {
        private static int _major = 4;
        private static int _minor = 1;
        public static new BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
        public override BoltProtocolVersion GetVersion() { return Version; }

		protected override IMessageFormat MessageFormat { get { return BoltProtocolMessageFormat.V4_1; } }
		protected virtual IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth,
														IDictionary<string, string> routingContext)
		{
			return new HelloMessage(userAgent, auth, routingContext);
		}

		protected override IResponseHandler GetHelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }

		protected IDictionary<string, string> RoutingContext { get; set; }

        protected BoltProtocolV4_1()
        {
        }

        public BoltProtocolV4_1(IDictionary<string, string> routingContext)
        {
            RoutingContext = routingContext;
        }

        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection.EnqueueAsync(HelloMessage(userAgent, authToken.AsDictionary(), RoutingContext),
										  GetHelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

    }
}
