using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.V4_1;
using Neo4j.Driver.Internal.Messaging.V4_1;


namespace Neo4j.Driver.Internal.Protocol
{
    class BoltProtocolV4_1 : BoltProtocolV4
    {
        public static readonly BoltProtocolV4_1 BoltV4_1 = new BoltProtocolV4_1();

        public override BoltProtocolVersion Version()
        {
            return new BoltProtocolVersion(4, 1);
        }

        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection
                .EnqueueAsync(new HelloMessage(userAgent, authToken.AsDictionary()),
                    new HelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

    }
}
