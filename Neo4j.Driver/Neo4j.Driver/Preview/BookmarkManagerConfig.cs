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

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Experimental;

/// <summary>
/// The <see cref="BookmarkManagerConfig"/> record encapsulates configuration values for initializing a new
/// default <see cref="IBookmarkManager"/> implementation. The <see cref="BookmarkManagerConfig"/> instance should be
/// passed to an <see cref="IBookmarkManagerFactory.NewBookmarkManager"/> factory method to construct a new
/// <see cref="IBookmarkManager"/> instance.
/// </summary>
/// <remarks>
/// The default <see cref="IBookmarkManagerFactory"/> can be accessed from
/// <see cref="Experimental.GraphDatabase.BookmarkManagerFactory"/>.
/// </remarks>
public record BookmarkManagerConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookmarkManagerConfig"/> record using the optional bookmark
    /// collection and delegate parameters.
    /// </summary>
    /// <param name="initialBookmarks">The initial bookmarks to populate a new <see cref="IBookmarkManager"/> with.</param>
    /// <param name="bookmarkSupplierAsync">
    /// A function for supplying the <see cref="IBookmarkManager"/> instance with bookmarks
    /// from user code.<br/> The function is invoked when updating routing tables, beginning transactions, and running queries
    /// in sessions configured with the <see cref="IBookmarkManager"/> instance.
    /// </param>
    /// <param name="notifyBookmarksAsync">
    /// A function for the <see cref="IBookmarkManager"/> instance to notify user code of
    /// new bookmarks received by sessions configured with the <see cref="IBookmarkManager"/>.<br/> The function is invoked
    /// when a transaction completes or session query completes in sessions configured with the <see cref="IBookmarkManager"/>
    /// instance.
    /// </param>
    public BookmarkManagerConfig(
        IEnumerable<string>? initialBookmarks = null,
        Func<CancellationToken, Task<string[]>>? bookmarkSupplierAsync = null,
        Func<string[], CancellationToken, Task>? notifyBookmarksAsync = null)
    {
        InitialBookmarks = initialBookmarks;
        BookmarkSupplierAsync = bookmarkSupplierAsync;
        NotifyBookmarksAsync = notifyBookmarksAsync;
    }

    /// <summary>Gets the collection of initial bookmarks to provide the <see cref="IBookmarkManager"/>.</summary>
    public IEnumerable<string>? InitialBookmarks { get; }

    /// <summary>Gets the function for supplying the <see cref="IBookmarkManager"/> with bookmarks from user code.</summary>
    public Func<CancellationToken, Task<string[]>>? BookmarkSupplierAsync { get; }

    /// <summary>Gets the function to notify user code of new bookmarks received by the <see cref="IBookmarkManager"/>.</summary>
    public Func<string[], CancellationToken, Task>? NotifyBookmarksAsync { get; }
}
