
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_3;
using V4_3 = Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.Messaging.V3;
using V3 = Neo4j.Driver.Internal.MessageHandling.V3;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_3 : BoltProtocolV4_2
    {
        private static int _major = 4;
        private static int _minor = 3;
        public static new BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
        public override BoltProtocolVersion GetVersion() { return Version; }

        private IDictionary<string, string> RoutingContext { get; set; }

        protected BoltProtocolV4_3()
        {
        }

        public BoltProtocolV4_3(IDictionary<string, string> routingContext)
        {
            RoutingContext = routingContext;
        }

        public override IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V4_3);
        }

        public override IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V4_3);
        }

        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection
                .EnqueueAsync(new V4_3.HelloMessage(userAgent, authToken.AsDictionary(), RoutingContext),
                    new HelloResponseHandler(connection, Version)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, Bookmark bookmark)
        {
            connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

            var responseHandler = new RouteResponseHandler();

            await connection.EnqueueAsync(new V4_3.RouteMessage(connection.RoutingContext, bookmark, database), responseHandler).ConfigureAwait(false);

            await connection.SyncAsync().ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);

            return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;            
        }
	}
}
