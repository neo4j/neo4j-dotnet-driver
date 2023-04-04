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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Preview;

/// <summary>Configuration for running queries using the simplified api.</summary>
public class QueryConfig
{
    /// <summary>Construct new instance for configuration for running queries using the simplified API.</summary>
    /// <param name="routing">Routing for query.</param>
    /// <param name="database">Database name of database query should be executed against.</param>
    /// <param name="impersonatedUser">Username of a user to impersonate while executing a query.</param>
    /// <param name="bookmarkManager">
    /// Instance of <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and
    /// receive resulting bookmarks.<br/> When null the driver will use it's own <see cref="IBookmarkManager"/> for causal
    /// chaining.
    /// </param>
    /// <param name="enableBookmarkManager">
    /// Whether or not to use a <see cref="IBookmarkManager"/>, setting to false will
    /// remove any usage or modification of <see cref="IBookmarkManager"/>.
    /// </param>
    public QueryConfig(
        RoutingControl routing = RoutingControl.Writers,
        string database = null,
        string impersonatedUser = null,
        IBookmarkManager bookmarkManager = null,
        bool enableBookmarkManager = true)
    {
        Routing = routing;
        Database = database;
        ImpersonatedUser = impersonatedUser;
        BookmarkManager = bookmarkManager;
        EnableBookmarkManager = enableBookmarkManager;
    }

    /// <summary>Members of the cluster the query can be processed by.</summary>
    public RoutingControl Routing { get; }

    /// <summary>Database to execute the query against.</summary>
    public string Database { get; }

    /// <summary>User to impersonate while executing a query.</summary>
    public string ImpersonatedUser { get; }

    /// <summary>
    /// <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and receive resulting bookmarks.<br/>
    /// Can be disabled by setting <see cref="EnableBookmarkManager"/> to false.
    /// </summary>
    public IBookmarkManager BookmarkManager { get; }

    /// <summary>Enables or disables the use of an <see cref="IBookmarkManager"/> for causal chaining.</summary>
    public bool EnableBookmarkManager { get; }
}

/// <summary>Configuration for running queries using the simplified API.</summary>
public class QueryConfig<T> : QueryConfig
{
    /// <summary>Construct new instance for configuration for running queries using the simplified API.</summary>
    /// <param name="cursorProcessor">
    /// Function for processing an <see cref="IResultCursor"/>.<br/> The cursor processor will
    /// execute inside the scope of an open transaction.<br/> Raising any exception will cause the transaction to rollback.
    /// </param>
    /// <param name="routing">Routing for query.</param>
    /// <param name="database">Database name of database query should be executed against.</param>
    /// <param name="impersonatedUser">Username of a user to impersonate while executing a query.</param>
    /// <param name="bookmarkManager">
    /// Instance of <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and
    /// receive resulting bookmarks.
    /// </param>
    /// <param name="enableBookmarkManager">
    /// Whether or not to use an <see cref="IBookmarkManager"/>, setting to false will
    /// remove any usage or modification of <see cref="IBookmarkManager"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public QueryConfig(
        Func<IResultCursor, CancellationToken, Task<T>> cursorProcessor,
        RoutingControl routing = RoutingControl.Writers,
        string database = null,
        string impersonatedUser = null,
        IBookmarkManager bookmarkManager = null,
        bool enableBookmarkManager = true)
        : base(routing, database, impersonatedUser, bookmarkManager, enableBookmarkManager)
    {
        CursorProcessor = cursorProcessor ?? throw new ArgumentNullException(nameof(cursorProcessor));
    }

    /// <summary>
    /// Configures a function for processing an <see cref="IResultCursor"/>.<br/> The cursor processor will execute
    /// inside the scope of an open transaction.<br/> Raising any exception will cause the transaction to rollback.
    /// </summary>
    public Func<IResultCursor, CancellationToken, Task<T>> CursorProcessor { get; init; }
}
