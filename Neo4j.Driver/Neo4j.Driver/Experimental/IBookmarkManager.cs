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
/// Experimental: Subject to change.<br/>
/// The <see cref="IBookmarkManager"/> interface is intended for implementation by classes that provide convenient
/// interfacing with <see cref="Bookmarks"/> in both the driver and user code.
/// </summary>
/// <see cref="ExperimentalExtensions.WithBookmarkManager"/>
public interface IBookmarkManager
{
    /// <summary>
    /// Updates the <see cref="IBookmarkManager"/>'s internal cache, removing values in
    /// <paramref name="previousBookmarks"/> and inserting values in <paramref name="newBookmarks"/>.<br/>
    /// After the <see cref="IBookmarkManager"/> updates, it invokes the configured
    /// <see cref="BookmarkManagerConfig.NotifyBookmarksAsync"/> function with the latest known bookmarks.
    /// </summary>
    /// <param name="previousBookmarks">
    /// The bookmarks used at beginning of the causally chained process such as
    /// <see cref="IAsyncTransaction.CommitAsync"/>.
    /// </param>
    /// <param name="newBookmarks">The Bookmarks received from completing a causally chained process such as
    /// <see cref="IAsyncTransaction.CommitAsync"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// An asynchronous task that completes when the <see cref="IBookmarkManager"/> has been updated and the optional
    /// <see cref="BookmarkManagerConfig.NotifyBookmarksAsync"/> function has completed.
    /// </returns>
    Task UpdateBookmarksAsync(string[] previousBookmarks, string[] newBookmarks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all bookmarks from the internal cache, combining them with configured
    /// <see cref="BookmarkManagerConfig.BookmarkSupplierAsync"/>.<br/>
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// An asynchronous task that completes when the <see cref="IBookmarkManager"/> has collected bookmarks
    /// from both the internal cache and the optional <see cref="BookmarkManagerConfig.BookmarkSupplierAsync"/>
    /// function.
    /// </returns>
    Task<string[]> GetBookmarksAsync(CancellationToken cancellationToken = default);
}