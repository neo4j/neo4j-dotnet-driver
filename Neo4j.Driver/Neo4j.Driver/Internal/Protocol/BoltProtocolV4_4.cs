using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling.V4_4;
using Neo4j.Driver.Internal.Messaging.V4_4;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;


namespace Neo4j.Driver.Internal.Protocol
{
	class BoltProtocolV4_4 : BoltProtocolV4_3
	{
		private static int _major = 4;
		private static int _minor = 4;
        public const string BoltPatchKey = "patch_bolt";
        public static new BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
		public override BoltProtocolVersion GetVersion() { return Version; }
		protected override IMessageFormat MessageFormat { get { return BoltProtocolMessageFormat.V4_4; } }

		protected override IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth,
														IDictionary<string, string> routingContext)
		{
			return new HelloMessage(userAgent, auth, routingContext);
		}
				
		protected override IRequestMessage GetBeginMessage(string database, Bookmark bookmark, TransactionConfig config, AccessMode mode, string impersonatedUser)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			return new BeginMessage(database, bookmark, config?.Timeout, config?.Metadata, mode, impersonatedUser);
		}

		protected override IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmark bookmark = null, TransactionConfig config = null,
            AccessMode mode = AccessMode.Write, string database = null, string impersonatedUser = null)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser); 
			return new RunWithMetadataMessage(query, database, bookmark, config, mode, impersonatedUser);
		}

		protected override IResponseHandler GetHelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn, Version); }


		public BoltProtocolV4_4(IDictionary<string, string> routingContext)
			: base(routingContext)
		{

		}

        public override IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncoded = false)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, 
                useUtcEncoded ? BoltProtocolMessageFormat.V4_4Utc : BoltProtocolMessageFormat.V4_4);
        }

        public override IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncoded = false)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, 
                useUtcEncoded ? BoltProtocolMessageFormat.V4_4Utc : BoltProtocolMessageFormat.V4_4);
        }

        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmark bookmark)
		{
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			var responseHandler = new RouteResponseHandler();

			await connection.EnqueueAsync(new RouteMessage(connection.RoutingContext, bookmark, database, impersonatedUser), responseHandler).ConfigureAwait(false);

			try
			{
				await connection.SyncAsync().ConfigureAwait(false);
			}
			finally
			{
				await connection.CloseAsync().ConfigureAwait(false);
			}

			return (IReadOnlyDictionary<string, object>)responseHandler.RoutingInformation;
		}

		protected override void ValidateImpersonatedUserForVersion(string impersonatedUser)
		{
			//do nothing as all values of impersonated user string are valid for version 4.4 onwards.
		}
	}
}

