// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal;

internal interface IBoltProtocol
{
    Task LoginAsync(IConnection connection, string userAgent, IAuthToken authToken,
        INotificationsConfig notificationsConfig);
    Task LogoutAsync(IConnection connection);
    Task ResetAsync(IConnection connection);

    Task<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        IConnection connection,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks);

    Task<IResultCursor> RunInAutoCommitTransactionAsync(IConnection connection, AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig);

    Task BeginTransactionAsync(
        IConnection connection,
        string database,
        Bookmarks bookmarks,
        TransactionConfig config,
        string impersonatedUser,
        INotificationsConfig notificationsConfig);

    Task<IResultCursor> RunInExplicitTransactionAsync(
        IConnection connection,
        Query query,
        bool reactive,
        long fetchSize = Config.Infinite);

    Task CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker);
    Task RollbackTransactionAsync(IConnection connection);
}
