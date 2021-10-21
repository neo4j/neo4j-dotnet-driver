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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal.MessageHandling;
using static Neo4j.Driver.Internal.Messaging.PullAllMessage;
using Neo4j.Driver.Internal.MessageHandling.V3;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV3 : IBoltProtocol
    {
        private const string GetRoutingTableProcedure = "CALL dbms.cluster.routing.getRoutingTable($context)";

		private static int _major = 3;
        private static int _minor = 0;
        public static BoltProtocolVersion Version { get; } = new BoltProtocolVersion(_major, _minor);
        public virtual BoltProtocolVersion GetVersion() { return Version; }

		protected virtual IMessageFormat MessageFormat { get { return BoltProtocolMessageFormat.V3; } }
		protected virtual IRequestMessage HelloMessage(string userAgent,
														IDictionary<string, object> auth)
		{
			return new Messaging.V3.HelloMessage(userAgent, auth);
		}
		protected virtual IResponseHandler HelloResponseHandler(IConnection conn) { return new HelloResponseHandler(conn); }

		public BoltProtocolV3()
        {

        }

        public virtual IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null)
        {
            return new MessageWriter(writeStream, bufferSettings.DefaultWriteBufferSize, bufferSettings.MaxWriteBufferSize, logger, MessageFormat);
        }

        public virtual IMessageReader NewReader(Stream stream, BufferSettings bufferSettings,
            ILogger logger = null)
        {
            return new MessageReader(stream, bufferSettings.DefaultReadBufferSize, bufferSettings.MaxReadBufferSize, logger, MessageFormat);
        }

        public virtual async Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken)
        {
            await connection.EnqueueAsync(HelloMessage(userAgent, authToken.AsDictionary()),
										  HelloResponseHandler(connection)).ConfigureAwait(false);
            await connection.SyncAsync().ConfigureAwait(false);
        }

        public virtual async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
                                                                                 Query query, 
                                                                                 bool reactive, 
                                                                                 IBookmarkTracker bookmarkTracker,
                                                                                 IResultResourceHandler resultResourceHandler,
                                                                                 string database, 
                                                                                 Bookmark bookmark, 
                                                                                 TransactionConfig config, 
                                                                                 long fetchSize = Config.Infinite)
        {
            AssertNullDatabase(database);

            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, resultResourceHandler);
            var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, bookmarkTracker);
            await connection.EnqueueAsync(new RunWithMetadataMessage(query, bookmark, config, connection.GetEnforcedAccessMode()), runHandler, PullAll, pullAllHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public virtual async Task BeginTransactionAsync(IConnection connection, string database, Bookmark bookmark, TransactionConfig config)
        {
			AssertNullDatabase(database);

			await connection.EnqueueAsync(new BeginMessage(bookmark,
														   config,
														   connection.GetEnforcedAccessMode()),
											new BeginResponseHandler()
										  ).ConfigureAwait(false);
			
			await connection.SyncAsync().ConfigureAwait(false);
		}

        public virtual async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection, Query query, bool reactive, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync, null, null, null);
            var runHandler = new RunResponseHandler(streamBuilder, summaryBuilder);
            var pullAllHandler = new PullResponseHandler(streamBuilder, summaryBuilder, null);
            await connection.EnqueueAsync(new RunWithMetadataMessage(query), runHandler, PullAll, pullAllHandler).ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public async Task CommitTransactionAsync(IConnection connection, IBookmarkTracker bookmarkTracker)
        {
            await connection.EnqueueAsync(CommitMessage.Commit, new CommitResponseHandler(bookmarkTracker))
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
            if (database != null)
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

        public virtual async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmark bookmark)
		{
            connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

			connection.Mode = AccessMode.Read;

            string procedure;
            var parameters = new Dictionary<string, object>();

            var bookmarkTracker = new BookmarkTracker(bookmark);
            var resourceHandler = new ConnectionResourceHandler(connection);
            var sessionDb = connection.SupportsMultidatabase() ? "system" : null;

            GetProcedureAndParameters(connection, database, out procedure, out parameters);            
            var query = new Query(procedure, parameters);

            var result = await RunInAutoCommitTransactionAsync(connection, query, false, bookmarkTracker, resourceHandler, sessionDb, bookmark, null).ConfigureAwait(false);
            var record = await result.SingleAsync();

            return record.Values;
        }

        private class ConnectionResourceHandler : IResultResourceHandler
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

        private class BookmarkTracker : IBookmarkTracker
        {
            private Bookmark InternalBookmark { get; set; }

            public BookmarkTracker(Bookmark bookmark)
            {
                InternalBookmark = bookmark;
            }

            public void UpdateBookmark(Bookmark bookmark)
            {
                if (InternalBookmark != null && InternalBookmark.Values.Any())
                {
                    InternalBookmark = bookmark;
                }
            }
        }
    }
}