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
//TODO: Update comments!

/// <summary>
/// Configuration for running queries using <see cref="IDriver.ExecuteQueryAsync"/>
/// </summary>
public class QueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public RoutingControl Routing { get; }

    /// <summary>
    /// 
    /// </summary>
    public string Database { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public string ImpersonatedUser { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public IBookmarkManager BookmarkManager { get; }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="routing"></param>
    /// <param name="database"></param>
    /// <param name="impersonatedUser"></param>
    /// <param name="bookmarkManager"></param>
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
    /// 
    /// </summary>
    /// <param name="cursorProcessor"></param>
    /// <param name="routing"></param>
    /// <param name="database"></param>
    /// <param name="impersonatedUser"></param>
    /// <param name="bookmarkManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public QueryConfig(Func<IResultCursor, CancellationToken, Task<T>> cursorProcessor, RoutingControl routing = RoutingControl.Writers, string database = null,
        string impersonatedUser = null, IBookmarkManager bookmarkManager = null) 
        : base(routing, database, impersonatedUser, bookmarkManager)
    {
        CursorProcessor = cursorProcessor ?? throw new ArgumentNullException(nameof(cursorProcessor));
    }
    /// <summary>
    /// 
    /// </summary>  
    public Func<IResultCursor, CancellationToken, Task<T>> CursorProcessor { get; init; }
}
