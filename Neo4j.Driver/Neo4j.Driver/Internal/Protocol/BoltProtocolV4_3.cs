
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{	class BoltProtocolV4_3 : BoltProtocolV4_2
	{
		private static int _major = 4;
		private static int _minor = 3;
		public static new BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
		public override BoltProtocolVersion GetVersion() { return Version; }

		protected override IMessageFormat MessageFormat { get { return BoltProtocolMessageFormat.V4_3; } }
		protected override IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth,
														IDictionary<string, string> routingContext)
		{
			return new HelloMessage(userAgent, auth, routingContext);
		}

		protected override IResponseHandler HelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }


		protected BoltProtocolV4_3()
		{
		}

		public BoltProtocolV4_3(IDictionary<string, string> routingContext) : base(routingContext)
		{
		}

		public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmark bookmark)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(new RouteMessage(connection.RoutingContext, bookmark, database), responseHandler).ConfigureAwait(false);

			await connection.SyncAsync().ConfigureAwait(false);
			await connection.CloseAsync().ConfigureAwait(false);

			//Since 4.4 the Routing information will contain a db. 4.3 needs to populate this here as it's not received in the older route response...
			responseHandler.RoutingInformation.Add("db", database);

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}
	}
}