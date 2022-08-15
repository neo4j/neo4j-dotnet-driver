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
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V3;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Messaging.V4;
using static Neo4j.Driver.Internal.Messaging.V4.ResultHandleMessage;
using V3 = Neo4j.Driver.Internal.MessageHandling.V3;
using V4 = Neo4j.Driver.Internal.MessageHandling.V4;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV4_0 : BoltProtocolV3
    {
        private const string GetRoutingTableForDatabaseProcedure = "CALL dbms.routing.getRoutingTable($context, $database)";
        public override BoltProtocolVersion Version => BoltProtocolVersion.V4_0;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V4;
		
        protected override IRequestMessage GetHelloMessage(string userAgent, IDictionary<string, object> auth)
		{
			return new HelloMessage(userAgent, auth);
		}

		protected override IRequestMessage GetBeginMessage(string database, Bookmarks bookmarks, TransactionConfig config, AccessMode mode, string impersonatedUser)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);

			return new BeginMessage(database, bookmarks, config?.Timeout, config?.Metadata, mode);
		}

		protected override IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmarks bookmarks = null, TransactionConfig config = null, AccessMode mode = AccessMode.Write, string database = null, string impersonatedUser = null)
		{
			ValidateImpersonatedUserForVersion(impersonatedUser);

			return new RunWithMetadataMessage(query, database, bookmarks, config, mode);
		}

		protected override IResponseHandler GetHelloResponseHandler(IConnection conn) { return new V3.HelloResponseHandler(conn); }


        public override async Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection,
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
            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync,
                RequestMore(connection, summaryBuilder, bookmarksTracker),
                CancelRequest(connection, summaryBuilder, bookmarksTracker),
                resultResourceHandler,
                fetchSize, reactive);
            var runHandler = new V4.RunResponseHandler(streamBuilder, summaryBuilder);

            var pullMessage = default(PullMessage);
            var pullHandler = default(V4.PullResponseHandler);
            if (!reactive)
            {
                pullMessage = new PullMessage(fetchSize);
                pullHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
            }

            await connection
                .EnqueueAsync(
                    GetRunWithMetaDataMessage(query, bookmarks, config,
                        connection.GetEnforcedAccessMode(), database, impersonatedUser), runHandler,
                    pullMessage, pullHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        public override async Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection,
            Query query, bool reactive, long fetchSize = Config.Infinite)
        {
            var summaryBuilder = new SummaryBuilder(query, connection.Server);
            var streamBuilder = new ResultCursorBuilder(summaryBuilder, connection.ReceiveOneAsync,
                RequestMore(connection, summaryBuilder, null),
                CancelRequest(connection, summaryBuilder, null), null,
                fetchSize, reactive);
            var runHandler = new V4.RunResponseHandler(streamBuilder, summaryBuilder);

            var pullMessage = default(PullMessage);
            var pullHandler = default(V4.PullResponseHandler);
            if (!reactive)
            {
                pullMessage = new PullMessage(fetchSize);
                pullHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, null);
            }

            await connection.EnqueueAsync(GetRunWithMetaDataMessage(query),
                    runHandler, pullMessage, pullHandler)
                .ConfigureAwait(false);
            await connection.SendAsync().ConfigureAwait(false);
            return streamBuilder.CreateCursor();
        }

        private static Func<IResultStreamBuilder, long, long, Task> RequestMore(IConnection connection,
            SummaryBuilder summaryBuilder, IBookmarksTracker bookmarksTracker)
        {
            return async (streamBuilder, id, n) =>
            {
                var pullAllHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
                await connection
                    .EnqueueAsync(new PullMessage(id, n), pullAllHandler)
                    .ConfigureAwait(false);
                await connection.SendAsync().ConfigureAwait(false);
            };
        }

        private static Func<IResultStreamBuilder, long, Task> CancelRequest(IConnection connection,
            SummaryBuilder summaryBuilder, IBookmarksTracker bookmarksTracker)
        {
            return async (streamBuilder, id) =>
            {
                var pullAllHandler = new V4.PullResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
                await connection
                    .EnqueueAsync(new DiscardMessage(id, All), pullAllHandler)
                    .ConfigureAwait(false);
                await connection.SendAsync().ConfigureAwait(false);
            };
        }

        protected internal override void GetProcedureAndParameters(IConnection connection, string database, out string procedure, out Dictionary<string, object> parameters)
        {
            procedure = GetRoutingTableForDatabaseProcedure;
            parameters = new Dictionary<string, object> { { "context", connection.RoutingContext }, { "database", string.IsNullOrEmpty(database) ? null : database } };
        }

        public override async Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection, string database, string impersonatedUser, Bookmarks bookmarks)
        {
            ValidateImpersonatedUserForVersion(impersonatedUser);
            connection = connection ?? throw new ProtocolException("Attempting to get a routing table on a null connection");

            connection.Mode = AccessMode.Read;

            string procedure;
            var parameters = new Dictionary<string, object>();

            var bookmarkTracker = new BookmarksTracker(bookmarks);
            var resourceHandler = new ConnectionResourceHandler(connection);
            var sessionDb = connection.SupportsMultidatabase() ? "system" : null;

            GetProcedureAndParameters(connection, database, out procedure, out parameters);
            var query = new Query(procedure, parameters);

            var result = await RunInAutoCommitTransactionAsync(connection, query, false, bookmarkTracker, resourceHandler, sessionDb, bookmarks, null, null).ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);

            //Since 4.4 the Routing information will contain a db. Earlier versions need to populate this here as it's not received in the older route response...
            var finalDictionary = record.Values.ToDictionary();
            finalDictionary[RoutingTableDBKey] = database;

            return (IReadOnlyDictionary<string, object>)finalDictionary;
        }
    }
}
