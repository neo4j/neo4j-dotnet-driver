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

using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal.Protocol
{
    internal interface IBoltProtocol
    {
        IMessageReader NewReader(Stream stream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncodedDateTimes = false);
        IMessageWriter NewWriter(Stream writeStream, BufferSettings bufferSettings, ILogger logger = null, bool useUtcEncodedDateTimes = false);

        Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken);

        Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection, 
                                                            Query query,
                                                            bool reactive, 
                                                            IBookmarksTracker bookmarksTracker, 
                                                            IResultResourceHandler resultResourceHandler,
                                                            string database, 
                                                            Bookmarks bookmark, 
                                                            TransactionConfig config,
															string impersonatedUser,
															long fetchSize);

        Task BeginTransactionAsync(IConnection connection, string database, Bookmarks bookmark, TransactionConfig config, string impersonatedUser);

        Task<IResultCursor> RunInExplicitTransactionAsync(IConnection connection, Query query, bool reactive, long fetchSize);

        Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker);

        Task RollbackTransactionAsync(IConnection connection);

        Task ResetAsync(IConnection connection);

        Task LogoutAsync(IConnection connection);

        BoltProtocolVersion Version { get; }

        Task<IReadOnlyDictionary<string, object>> GetRoutingTable(IConnection connection,
																  string database,
																  string impersonated_user,
																  Bookmarks bookmark); 
    }

}