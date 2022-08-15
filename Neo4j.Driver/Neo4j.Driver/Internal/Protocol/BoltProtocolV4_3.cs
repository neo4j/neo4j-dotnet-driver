using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_3 : BoltProtocolV4_2
	{
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_3;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4_3;
        protected virtual IMessageFormat UtcMessageFormat => BoltProtocolMessageFormat.V4_3Utc;
        public const string BoltPatchKey = "patch_bolt";

        public BoltProtocolV4_3(IDictionary<string, string> routingContext) : base(routingContext)
		{
		}

        protected override IRequestMessage HelloMessage(string userAgent,
            IDictionary<string, object> auth,
            IDictionary<string, string> routingContext)
        {
            return new HelloMessage(userAgent, auth, routingContext);
        }

        protected override IResponseHandler GetHelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }

        public override IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncoded = false)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, useUtcEncoded ? UtcMessageFormat : MessageFormat);
        }

        public override IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncoded = false)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, useUtcEncoded ? UtcMessageFormat : MessageFormat);
        }

        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(new RouteMessage(connection.RoutingContext, bookmarks, database), responseHandler).ConfigureAwait(false);

			await connection.SyncAsync().ConfigureAwait(false);
			await connection.CloseAsync().ConfigureAwait(false);

			//Since 4.4 the Routing information will contain a db. 4.3 needs to populate this here as it's not received in the older route response...
			responseHandler.RoutingInformation.Add(RoutingTableDBKey, database);

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}
	}
}
