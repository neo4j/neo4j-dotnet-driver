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
            IDictionary<string, string> routingContext)
        {
            return new HelloMessage(userAgent, auth, routingContext);
        }

        protected override IResponseHandler GetHelloResponseHandler(IConnection conn) => new HelloResponseHandler(conn, Version);


        public override async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection.EnqueueAsync(HelloMessage(userAgent, authToken.AsDictionary(), RoutingContext),
										  GetHelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }


        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmark bookmark)
        {
            ValidateImpersonatedUserForVersion(impersonatedUser);
            connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

            connection.Mode = AccessMode.Read;

            string procedure;
            var parameters = new Dictionary<string, object>();

            var bookmarkTracker = new BookmarkTracker(bookmark);
            var resourceHandler = new ConnectionResourceHandler(connection);
            var sessionDb = connection.SupportsMultidatabase() ? "system" : null;

            GetProcedureAndParameters(connection, database, out procedure, out parameters);
            var query = new Query(procedure, parameters);

            var result = await RunInAutoCommitTransactionAsync(connection, query, false, bookmarkTracker, resourceHandler, sessionDb, bookmark, null, null).ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);

            //Since 4.4 the Routing information will contain a db. Earlier versions need to populate this here as it's not received in the older route response...
            var finalDictionary = record.Values.ToDictionary();
            finalDictionary[RoutingTableDBKey] = database;

            return (IReadOnlyDictionary<string, object>)finalDictionary;
        }

    }
}
