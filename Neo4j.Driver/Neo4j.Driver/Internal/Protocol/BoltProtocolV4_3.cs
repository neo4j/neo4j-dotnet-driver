using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using Neo4j.Driver.Internal.Messaging.V4_3;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_3 : BoltProtocolV4_0
	{
        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(
                new RouteMessage(connection.RoutingContext, bookmarks, database), 
                responseHandler)
                .ConfigureAwait(false);

			await connection.SyncAsync().ConfigureAwait(false);
			await connection.CloseAsync().ConfigureAwait(false);

			//Since 4.4 the Routing information will contain a db. 4.3 needs to populate this here as it's not received in the older route response...
			responseHandler.RoutingInformation.Add(RoutingTableDbKey, database);

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}
	}
}
