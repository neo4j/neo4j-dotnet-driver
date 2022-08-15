using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_4;
using Neo4j.Driver.Internal.Messaging.V4_4;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_4 : BoltProtocolV4_3
	{
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_4;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_4;
        protected override IMessageFormat UtcMessageFormat => BoltProtocolMessageFormat.V4_4Utc;
        public const string BoltPatchKey = "patch_bolt";
        public BoltProtocolV4_4(IDictionary<string, string> routingContext)
            : base(routingContext)
        {
        }

        protected override IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth,
														IDictionary<string, string> routingContext)
		{
			return new HelloMessage(userAgent, auth, routingContext);
		}
				
		protected override IRequestMessage GetBeginMessage(string database, Bookmarks bookmarks, TransactionConfig config, AccessMode mode, string impersonatedUser)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			return new BeginMessage(database, bookmarks, config?.Timeout, config?.Metadata, mode, impersonatedUser);
		}

		protected override IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmarks bookmarks = null, TransactionConfig config = null, AccessMode mode = AccessMode.Write, string database = null, string impersonatedUser = null)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			return new RunWithMetadataMessage(query, database, bookmarks, config, mode, impersonatedUser);
		}

		protected override IResponseHandler GetHelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }

		public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(new RouteMessage(connection.RoutingContext, bookmarks, database, impersonatedUser), responseHandler).ConfigureAwait(false);

			await connection.SyncAsync().ConfigureAwait(false);
			await connection.CloseAsync().ConfigureAwait(false);

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}

		protected override void ValidateImpersonatedUserForVersion(string impersonatedUser)
		{
			//do nothing as all values of impersonated user string are valid for version 4.4 onwards.
		}
	}
}

