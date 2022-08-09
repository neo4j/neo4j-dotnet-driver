// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using Neo4j.Driver.Internal.MessageHandling.V3;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3 : IBoltProtocol
    {
        private const string GetRoutingTableProcedure = "CALL dbms.cluster.routing.getRoutingTable($context)";
		protected const string RoutingTableDBKey = "db";

		protected virtual IMessageFormat MessageFormat  => BoltProtocolMessageFormat.V3;
        public virtual BoltProtocolVersion Version => BoltProtocolVersion.V3_0;

        protected virtual IRequestMessage GetHelloMessage(string userAgent,
														IDictionary<string, object> auth)
		{
			return new Messaging.V3.HelloMessage(userAgent, auth);
		}

		protected virtual IRequestMessage GetBeginMessage(string database, Bookmarks bookmarks, TransactionConfig config, AccessMode mode, string impersonatedUser)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			AssertNullDatabase(database);

			return new BeginMessage(bookmarks, config, mode);
		}

		protected virtual IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmarks bookmarks = null, TransactionConfig config = null, AccessMode mode = AccessMode.Write, string database = null, string impersonatedUser = null)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			return new RunWithMetadataMessage(query, bookmarks, config, mode);
		}

        protected virtual IResponseHandler GetHelloResponseHandler(IConnection conn) => new HelloResponseHandler(conn);

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings,
            ILogger logger = null, bool _ = false)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, MessageFormat);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings,
            ILogger logger = null, bool _ = false)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, MessageFormat);
        }

        public virtual async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection.EnqueueAsync(GetHelloMessage(userAgent, authToken.AsDictionary()),
										  GetHelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public virtual async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
                                                                                 Query query, 
                                                                                 bool reactive, 
                                                                                 IBookmarksTracker bookmarksTracker,
                                                                                 IResultResourceHandler resultResourceHandler,
                                                                                 string database, 
                                                                                 Bookmarks bookmarks, 
                                                                                 TransactionConfig config,
																				 string impersonatedUser,
																				 long fetchSize = Config.Infinite)
        {
            AssertNullDatabase(database);

            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, resultResourceHandler);
            var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            await connection.EnqueueAsync(GetRunWithMetaDataMessage(query, bookmarks, config, connection.GetEnforcedAccessMode(), null, impersonatedUser), runHandler, PullAll, pullAllHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public virtual async Task BeginTransactionAsync(IConnection connection, string database, Bookmarks bookmarks, TransactionConfig config, string impersonatedUser)
        {	
			await connection.EnqueueAsync(GetBeginMessage(database, 
													   bookmarks, 
													   config, 
													   connection.GetEnforcedAccessMode(), 
													   impersonatedUser),
										  new BeginResponseHandler())
							.ConfigureAwait(false);
			
			await connection.SyncAsync().ConfigureAwait(false);
		}

        public virtual async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection, Query query, bool reactive, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, null);
            var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, null);
            await connection.EnqueueAsync(GetRunWithMetaDataMessage(query), runHandler, PullAll, pullAllHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public async Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker)
        {
            await connection.EnqueueAsync(CommitMessage.Commit, new CommitResponseHandler(bookmarksTracker))
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public async Task RollbackTransactionAsync(IConnection connection)
        {
            await connection.EnqueueAsync(RollbackMessage.Rollback, new RollbackResponseHandler())
                .ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public Task ResetAsync(IConnection connection)
        {
            return connection.EnqueueAsync(ResetMessage.Reset, new ResetResponseHandler());
        }

        public async Task LogoutAsync(IConnection connection)
        {
            await connection.EnqueueAsync(GoodbyeMessage.Goodbye, new NoOpResponseHandler()).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
        }

        private void AssertNullDatabase(string database)
        {
			if(!string.IsNullOrEmpty(database))
            {
                throw new ClientException(
                    "Driver is connected to a server that does not support multiple databases. " +
                    "Please upgrade to neo4j 4.0.0 or later in order to use this functionality");
            }
        }

        protected internal virtual void GetProcedureAndParameters(IConnection connection, string database, out string procedure, out Dictionary<string, object> parameters)
		{
            procedure = GetRoutingTableProcedure;
            parameters = new Dictionary<string, object> { { "context", connection.RoutingContext } };
        }

        public virtual async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);
			connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			connection.Mode = AccessMode.Read;

            var bookmarkTracker = new BookmarksTracker(bookmarks);
            var resourceHandler = new ConnectionResourceHandler(connection);
            var sessionDb = connection.SupportsMultidatabase() ? "system" : null;

            GetProcedureAndParameters(connection, database, out var procedure, out var parameters);            
            var query = new Query(procedure, parameters);

            var result = await RunInAutoCommitTransactionAsync(connection, query, false, bookmarkTracker, resourceHandler, sessionDb, null, null, null).ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);

			//Since 4.4 the Routing information will contain a db. Earlier versions need to populate this here as it's not received in the older route response...
			var finalDictionary = record.Values.ToDictionary();
			finalDictionary[RoutingTableDBKey] = database;

			return (IReadOnlyDictionary<string, object>)finalDictionary;
        }

		protected virtual void ValidateImpersonatedUserForVersion(string impersonatedUser)
		{
			if (impersonatedUser is not null) throw new ArgumentException($"Boltprotocol {Version} does not support impersonatedUser, yet has been passed a non null impersonated user string");
		}

        protected class ConnectionResourceHandler : IResultResourceHandler
        {
            IConnection Connection { get; }
            public ConnectionResourceHandler(IConnection conn)
            {
                Connection = conn;
            }

            public Task OnResultConsumedAsync()
            {
                return CloseConnection();
            }

            private async Task CloseConnection()
            {
                await Connection.CloseAsync().ConfigureAwait(false);
            }			
		}

        protected class BookmarksTracker : IBookmarksTracker
        {
            private Bookmarks InternalBookmarks { get; set; }

            public BookmarksTracker(Bookmarks bookmarks)
            {
                InternalBookmarks = bookmarks;
            }

            public void UpdateBookmarks(Bookmarks bookmarks)
            {
                if (InternalBookmarks != null && InternalBookmarks.Values.Any())
                {
                    InternalBookmarks = bookmarks;
                }
            }
        }
    }
}
