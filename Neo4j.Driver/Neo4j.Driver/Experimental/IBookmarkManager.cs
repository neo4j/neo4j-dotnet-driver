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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// Experimental: Subject to change.
/// Manager of Neo4j's causal-consistency mechanism: <see cref="Bookmarks"/>.<br/>
/// The manager maintains and provides collections of bookmarks for all databases,
/// exposing to both driver and user-code.<br/>
/// </summary>
public interface IBookmarkManager
{
    /// <summary>
    /// Updates the bookmark manager's bookmark cache. Removing <paramref name="previousBookmarks"/>
    /// and inserting <paramref name="newBookmarks"/>.<br/>
    /// After cache completes updates it invokes configured <see cref="BookmarkManagerConfig.NotifyBookmarksAsync"/>
    /// with latest known bookmarks.
    /// </summary>
    /// <param name="previousBookmarks">Bookmarks used at beginning of transaction.</param>
    /// <param name="newBookmarks">Bookmarks received from transaction.</param>
    /// <param name="cancellationToken">Cancellation for async operation.</param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task UpdateBookmarksAsync(string[] previousBookmarks, string[] newBookmarks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all bookmarks from internal cache, combining with configured
    /// <see cref="BookmarkManagerConfig.BookmarkSupplierAsync"/>.<br/>
    /// </summary>
    /// <param name="cancellationToken">Cancellation for async operation.</param>
    /// <returns>A task that represents the asynchronous execution operation.<br/>
    /// Task's result contains last known bookmarks for database.</returns>
    Task<string[]> GetBookmarksAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes all bookmarks from internal cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation for async operation.</param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ForgetAsync(CancellationToken cancellationToken = default);
}