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
    internal class BoltProtocolV4_1 : BoltProtocolV4_0
    {
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_1;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_1;

		protected IDictionary<string, string> RoutingContext { get; set; }

        public BoltProtocolV4_1(IDictionary<string, string> routingContext)
        {
            RoutingContext = routingContext;
        }

        protected virtual IRequestMessage HelloMessage(string userAgent,
            IDictionary<string, object> auth,
            IDictionary<string, string> routingContext,
            INotificationFilterConfig[] _)
        {
            return new HelloMessage(userAgent, auth, routingContext);
        }

        protected override IResponseHandler GetHelloResponseHandler(IConnection conn) => new HelloResponseHandler(conn, Version);


        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken, INotificationFilterConfig[] notificationFilters = null)
        {
            await connection.EnqueueAsync(HelloMessage(userAgent, authToken.AsDictionary(), RoutingContext, notificationFilters),
										  GetHelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }
    }
}
