
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_1;
using Neo4j.Driver.Internal.Messaging.V4_1;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_1 : BoltProtocolV4
    {
        private IDictionary<string, string> RoutingContext { get; set; }

        private BoltProtocolV4_1()
        {
        }

        public BoltProtocolV4_1(IDictionary<string, string> routingContext)
        {
            RoutingContext = routingContext;
        }

        public override IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, BoltProtocolMessageFormat.V4_1);
        }

        public override IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, BoltProtocolMessageFormat.V4_1);
        }

        public override BoltProtocolVersion Version()
        {
            return new BoltProtocolVersion(4, 1);
        }

        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection
                .EnqueueAsync(new HelloMessage(userAgent, authToken.AsDictionary(), RoutingContext),
                    new HelloResponseHandler(connection, Version())).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

    }
}
