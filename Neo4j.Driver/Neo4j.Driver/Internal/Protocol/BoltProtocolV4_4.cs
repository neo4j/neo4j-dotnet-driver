using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_4;
using Neo4j.Driver.Internal.Messaging.V4_4;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_4 : BoltProtocolV4_3
	{
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_4;
        public override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_4;

		protected override IRequestMessage GetBeginMessage(string database, Bookmarks bookmarks, TransactionConfig config, AccessMode mode, string impersonatedUser)
		{
			return new BeginMessage(database, bookmarks, config?.Timeout, config?.Metadata, mode, impersonatedUser);
		}

		protected override IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmarks bookmarks = null, TransactionConfig config = null, AccessMode mode = AccessMode.Write, string database = null, string impersonatedUser = null)
		{
			return new RunWithMetadataMessage(query, database, bookmarks, config, mode, impersonatedUser);
		}

		public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(new RouteMessage(connection.RoutingContext, bookmarks, database, impersonatedUser), responseHandler).ConfigureAwait(false);

			await connection.SyncAsync().ConfigureAwait(false);
			await connection.CloseAsync().ConfigureAwait(false);

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}
	}
}

