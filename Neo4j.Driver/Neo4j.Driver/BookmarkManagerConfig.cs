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

namespace Neo4j.Driver;

/// <summary>
/// Configuration for constructing a default <see cref="IBookmarkManager"/> using
/// <see cref="IBookmarkManagerFactory.NewBookmarkManager"/>.<br/> the default <see cref="IBookmarkManagerFactory"/> can be
/// accessed from <see cref="GraphDatabase.BookmarkManagerFactory"/>.
/// </summary>
/// <param name="InitialBookmarks">
/// Nullable collection of initial bookmarks to provide the bookmark manager, the keys
/// should be database names.
/// </param>
/// <param name="BookmarkSupplierAsync">
/// Nullable delegate to provide externally sourced bookmarks to the driver.<br/>
/// Invoked when updating a cluster routing table, beginning transaction, or running a query from a session.<br/> the
/// argument will be either a database name when driver calls <see cref="IBookmarkManager.GetBookmarksAsync"/> or null when
/// the driver calls <see cref="IBookmarkManager.GetAllBookmarksAsync"/>.
/// </param>
/// <param name="NotifyBookmarksAsync">
/// Nullable delegate to notify application of new bookmarks received by the driver from
/// the server for a database.
/// </param>
public record BookmarkManagerConfig(
    Dictionary<string, IEnumerable<string>>? InitialBookmarks = null,
    Func<string?, CancellationToken, Task<string[]>>? BookmarkSupplierAsync = null,
    Func<string, string[], CancellationToken, Task>? NotifyBookmarksAsync = null);
