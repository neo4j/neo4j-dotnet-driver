// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Configuration for running queries using <see cref="IDriver.ExecuteQueryAsync"/>
/// </summary>
public class QueryConfig
{
    /// <summary>
    /// Configures which members of the cluster the query can be processed by.
    /// </summary>
    public RoutingControl Routing { get; }

    /// <summary>
    /// Configures which database to execute the query against.
    /// </summary>
    public string Database { get; }
    
    /// <summary>
    /// Configures a user to impersonate while executing a query.
    /// </summary>
    public string ImpersonatedUser { get; }
    
    /// <summary>
    /// Configures a <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and receive resulting bookmarks.
    /// </summary>
    public IBookmarkManager BookmarkManager { get; }

    /// <summary>
    /// Construct new instance for configuration for running queries using <see cref="IDriver.ExecuteQueryAsync"/>
    /// </summary>
    /// <param name="routing">Routing for query.</param>
    /// <param name="database">Database name of database query should be executed against.</param>
    /// <param name="impersonatedUser">Username of a user to impersonate while executing a query.</param>
    /// <param name="bookmarkManager">Instance of <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and receive resulting bookmarks.</param>
    public QueryConfig(RoutingControl routing = RoutingControl.Writers, string database = null, string impersonatedUser = null,
        IBookmarkManager bookmarkManager = null)
    {
        Routing = routing;
        Database = database;
        ImpersonatedUser = impersonatedUser;
        BookmarkManager = bookmarkManager;
    }
}

/// <summary>
/// Configuration for running queries using <see cref="IDriver.ExecuteQueryAsync{TResult}"/>
/// </summary>
public class QueryConfig<T> : QueryConfig
{
    /// <summary>
    /// Construct new instance for configuration for running queries using <see cref="IDriver.ExecuteQueryAsync{TResult}"/>.
    /// </summary>
    /// <param name="cursorProcessor">Function for processing an <see cref="IResultCursor"/>.<br/>
    /// The cursor processor will execute inside the scope of an open transaction.<br/>
    /// Raising any exception will cause the transaction to rollback.</param>
    /// <param name="routing">Routing for query.</param>
    /// <param name="database">Database name of database query should be executed against.</param>
    /// <param name="impersonatedUser">Username of a user to impersonate while executing a query.</param>
    /// <param name="bookmarkManager">Instance of <see cref="IBookmarkManager"/> to provide bookmarks for query execution, and receive resulting bookmarks.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public QueryConfig(Func<IResultCursor, CancellationToken, Task<T>> cursorProcessor, RoutingControl routing = RoutingControl.Writers, string database = null,
        string impersonatedUser = null, IBookmarkManager bookmarkManager = null) 
        : base(routing, database, impersonatedUser, bookmarkManager)
    {
        CursorProcessor = cursorProcessor ?? throw new ArgumentNullException(nameof(cursorProcessor));
    }

    /// <summary>
    /// Configures a function for processing an <see cref="IResultCursor"/>.<br/>
    /// The cursor processor will execute inside the scope of an open transaction.<br/>
    /// Raising any exception will cause the transaction to rollback.
    /// </summary>  
    public Func<IResultCursor, CancellationToken, Task<T>> CursorProcessor { get; init; }
}
