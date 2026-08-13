// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface IExecuteQueryConfigMapper
{
    QueryConfig Map(ExecuteQueryConfig config);
}

internal class ExecuteQueryConfigMapper : IExecuteQueryConfigMapper
{
    private readonly IObjectStore _objectStore;
    private readonly ICypherToNativeMapper _cypherToNativeMapper;

    public ExecuteQueryConfigMapper(IObjectStore objectStore, ICypherToNativeMapper cypherToNativeMapper)
    {
        _objectStore = objectStore;
        _cypherToNativeMapper = cypherToNativeMapper;
    }

    public QueryConfig Map(ExecuteQueryConfig config)
    {
        var routing = config.Routing == "r" ? RoutingControl.Readers : RoutingControl.Writers;

        var (bookmarkManager, enableBookmarkManager) = config.BookmarkManagerId switch
        {
            null => (default(IBookmarkManager), true),
            "-1" => (default(IBookmarkManager), false),
            var id => (_objectStore.Get<IBookmarkManager>(id).Object, true)
        };

        var transactionConfig = new TransactionConfig(
            config.TxMeta is not null ? _cypherToNativeMapper.Map(config.TxMeta) : null,
            config.Timeout is { } ms ? TimeSpan.FromMilliseconds(ms) : null);

        return new QueryConfig(
            routing,
            config.Database,
            config.ImpersonatedUser,
            bookmarkManager,
            enableBookmarkManager,
            transactionConfig,
            config.AuthorizationToken?.ToAuthToken());
    }
}
